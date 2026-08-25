using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.DTOs.Common;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;

namespace School.API.Repositories.Implementations
{
    public class SmsMessageRepository : ISmsMessageRepository
    {
        private readonly SchoolDbContext _context;

        public SmsMessageRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> AddAsync(SmsMessage message, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SmsMessage @VID={0}, @RecipientPhone={1}, @RecipientName={2}, @MessageText={3}, @MessageType={4}, @Status={5}, @SentAt={6}, @ErrorMessage={7}, @UserID={8}, @UserIP={9}",
                    0,
                    message.RecipientPhone,
                    message.RecipientName ?? (object)DBNull.Value,
                    message.MessageText,
                    (int)message.MessageType,
                    (int)message.Status,
                    message.SentAt ?? (object)DBNull.Value,
                    message.ErrorMessage ?? (object)DBNull.Value,
                    userId,
                    userIp)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task AddRangeAsync(
            IEnumerable<SmsMessage> messages,
            int userId,
            string userIp)
        {
            foreach (var message in messages)
            {
                await _context
                    .Set<ResponseDto>()
                    .FromSqlRaw(
                        "EXEC SpSave_SmsMessage @VID={0}, @RecipientPhone={1}, @RecipientName={2}, @MessageText={3}, @MessageType={4}, @Status={5}, @SentAt={6}, @ErrorMessage={7}, @UserID={8}, @UserIP={9}",
                        0,
                        message.RecipientPhone,
                        message.RecipientName ?? (object)DBNull.Value,
                        message.MessageText,
                        (int)message.MessageType,
                        (int)message.Status,
                        message.SentAt ?? (object)DBNull.Value,
                        message.ErrorMessage ?? (object)DBNull.Value,
                        userId,
                        userIp)
                    .ToListAsync();
            }
        }

        // History is queried with LINQ rather than SpGet_SmsMessages: the SP has a
        // fixed column list and would no longer satisfy the entity now that the
        // Meta template/delivery columns exist. The filters below match what the
        // SP applied, so callers see identical results.
        public async Task<IEnumerable<SmsMessage>> GetHistoryAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            return await BuildHistoryQuery(messageType, status, fromDate, toDate)
                .OrderByDescending(m => m.InsertedDate)
                .ThenByDescending(m => m.VID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetHistoryCountAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            return await BuildHistoryQuery(messageType, status, fromDate, toDate).CountAsync();
        }

        private IQueryable<SmsMessage> BuildHistoryQuery(
            SmsMessageType? messageType,
            SmsStatus? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.SmsMessages.AsNoTracking().AsQueryable();

            if (messageType.HasValue)
                query = query.Where(m => m.MessageType == messageType.Value);
            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);
            if (fromDate.HasValue)
                query = query.Where(m => m.InsertedDate >= fromDate.Value.Date);
            if (toDate.HasValue)
            {
                // Inclusive of the whole "to" day.
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(m => m.InsertedDate < toExclusive);
            }

            return query;
        }

        // ─────────────────────────────────────────────────────────────
        // Messages viewer — paged log with template + delivery detail
        // ─────────────────────────────────────────────────────────────
        public async Task<SmsMessagePagedDto> GetMessagesAsync(SmsMessageFilterDto filter)
        {
            int page = filter.Page < 1 ? 1 : filter.Page;
            int pageSize = filter.PageSize is < 1 or > 500 ? 50 : filter.PageSize;

            var query = BuildHistoryQuery(
                filter.MessageType, filter.Status, filter.FromDate, filter.ToDate);

            if (!string.IsNullOrWhiteSpace(filter.DeliveryStatus))
                query = query.Where(m => m.DeliveryStatus == filter.DeliveryStatus);

            if (!string.IsNullOrWhiteSpace(filter.TemplateName))
                query = query.Where(m => m.TemplateName == filter.TemplateName);

            if (!string.IsNullOrWhiteSpace(filter.JobId))
                query = query.Where(m => m.JobId == filter.JobId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(m =>
                    m.RecipientPhone.Contains(term) ||
                    (m.RecipientName != null && m.RecipientName.Contains(term)));
            }

            var total = await query.CountAsync();

            // Status tiles reflect the same filter set, so the numbers always
            // add up to the row count shown in the grid.
            var statusCounts = await query
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var rows = await query
                .OrderByDescending(m => m.InsertedDate)
                .ThenByDescending(m => m.VID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new SmsMessagePagedDto
            {
                Data = rows.Select(ToViewDto).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize,
                Pending = statusCounts.FirstOrDefault(s => s.Status == SmsStatus.Pending)?.Count ?? 0,
                Sent = statusCounts.FirstOrDefault(s => s.Status == SmsStatus.Sent)?.Count ?? 0,
                Failed = statusCounts.FirstOrDefault(s => s.Status == SmsStatus.Failed)?.Count ?? 0
            };
        }

        public async Task<SmsMessageViewDto?> GetMessageAsync(int vid)
        {
            var row = await _context.SmsMessages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.VID == vid);

            return row is null ? null : ToViewDto(row);
        }

        private static SmsMessageViewDto ToViewDto(SmsMessage m) => new()
        {
            VID = m.VID,
            RecipientPhone = m.RecipientPhone,
            RecipientName = m.RecipientName,
            MessageType = m.MessageType.ToString(),
            Status = m.Status.ToString(),
            MessageText = m.MessageText,
            FullMessage = m.FullMessage,
            TemplateName = m.TemplateName,
            TemplateLanguage = m.TemplateLanguage,
            TemplateParams = DeserializeParams(m.TemplateParams),
            Provider = m.Provider,
            ProviderMessageId = m.ProviderMessageId,
            DeliveryStatus = m.DeliveryStatus,
            ErrorCode = m.ErrorCode,
            ErrorMessage = m.ErrorMessage,
            ProviderResponse = m.ProviderResponse,
            SentAt = m.SentAt,
            DeliveredAt = m.DeliveredAt,
            ReadAt = m.ReadAt,
            InsertedDate = m.InsertedDate,
            JobId = m.JobId,
            // Result cards go out as a PDF document attached to the template.
            HasAttachment = m.MessageType == SmsMessageType.Rcard
        };

        private static List<string> DeserializeParams(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                // Corrupt/legacy value — show it raw rather than failing the page.
                return new List<string> { json };
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Queue methods — used by SmsQueueProcessor. These intentionally
        // bypass SpSave_SmsMessage because the SP does not know about
        // JobId; straight EF Core insert is simpler and transactional.
        // ─────────────────────────────────────────────────────────────
        public async Task EnqueueAsync(
            IEnumerable<SmsMessage> messages, int userId, string userIp)
        {
            var now = DateTime.UtcNow;
            var list = messages.ToList();
            foreach (var m in list)
            {
                m.VID = 0;
                m.Status = SmsStatus.Pending;
                m.SentAt = null;
                m.ErrorMessage = null;
                m.InsertedBy = userId;
                m.InsertedDate = now;
                m.InsertedIp = userIp;
            }
            _context.SmsMessages.AddRange(list);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SmsMessage>> GetPendingBatchAsync(int take)
        {
            return await _context.SmsMessages
                .Where(m => m.Status == SmsStatus.Pending)
                .OrderBy(m => m.InsertedDate)
                .ThenBy(m => m.VID)
                .Take(take)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(
            int vid, SmsStatus status, string? errorMessage)
        {
            var entity = await _context.SmsMessages.FindAsync(vid);
            if (entity == null) return;

            entity.Status = status;
            entity.ErrorMessage = errorMessage;
            if (status == SmsStatus.Sent)
                entity.SentAt = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Records the full provider outcome for one send. Called by
        // SmsQueueProcessor instead of UpdateStatusAsync whenever the message
        // went out through a template-aware sender.
        public async Task UpdateSendResultAsync(
            int vid,
            SmsStatus status,
            SmsSendOutcome outcome,
            string? provider,
            string? fullMessage,
            string? templateParamsJson)
        {
            var entity = await _context.SmsMessages.FindAsync(vid);
            if (entity == null) return;

            entity.Status = status;
            entity.ErrorMessage = Truncate(outcome.Error, 500);
            entity.ErrorCode = Truncate(outcome.ErrorCode, 50);
            entity.ProviderMessageId = Truncate(outcome.ProviderMessageId, 150);
            entity.Provider = Truncate(provider, 30);
            entity.ProviderResponse = Truncate(outcome.RawResponse, 4000);

            // The rendered body and parameters are only known at send time for
            // result messages, so persist them here when supplied.
            if (!string.IsNullOrWhiteSpace(fullMessage))
                entity.FullMessage = fullMessage;
            if (!string.IsNullOrWhiteSpace(templateParamsJson))
                entity.TemplateParams = templateParamsJson;

            if (status == SmsStatus.Sent)
            {
                entity.SentAt = DateTime.UtcNow;
                // Meta only confirms acceptance here; delivered/read arrive later
                // via webhook. Legacy gateways never report beyond this point.
                entity.DeliveryStatus = "accepted";
            }
            else if (status == SmsStatus.Failed)
            {
                entity.DeliveryStatus = "failed";
            }

            entity.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Meta delivery webhook -> the row holding that wamid.
        public async Task<bool> ApplyDeliveryStatusAsync(
            string providerMessageId,
            string deliveryStatus,
            DateTime timestamp,
            string? errorCode,
            string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(providerMessageId)) return false;

            var entity = await _context.SmsMessages
                .FirstOrDefaultAsync(m => m.ProviderMessageId == providerMessageId);
            if (entity == null) return false;

            entity.DeliveryStatus = deliveryStatus;

            switch (deliveryStatus)
            {
                case "delivered":
                    entity.DeliveredAt = timestamp;
                    break;
                case "read":
                    entity.ReadAt = timestamp;
                    // A read receipt implies delivery, which may not have arrived.
                    entity.DeliveredAt ??= timestamp;
                    break;
                case "failed":
                    // The message was accepted but WhatsApp could not deliver it
                    // (e.g. the number has no WhatsApp account) — reflect that in
                    // the coarse status so the viewer's Failed tile is accurate.
                    entity.Status = SmsStatus.Failed;
                    entity.ErrorCode = Truncate(errorCode, 50) ?? entity.ErrorCode;
                    entity.ErrorMessage = Truncate(errorMessage, 500) ?? entity.ErrorMessage;
                    break;
            }

            entity.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        public async Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId)
        {
            var dto = new SmsQueueStatusDto { JobId = jobId };
            if (string.IsNullOrWhiteSpace(jobId)) return dto;

            var grouped = await _context.SmsMessages
                .Where(m => m.JobId == jobId)
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var row in grouped)
            {
                switch (row.Status)
                {
                    case SmsStatus.Pending: dto.Pending = row.Count; break;
                    case SmsStatus.Sent:    dto.Sent    = row.Count; break;
                    case SmsStatus.Failed:  dto.Failed  = row.Count; break;
                }
            }
            dto.Total = dto.Pending + dto.Sent + dto.Failed;
            return dto;
        }

        public async Task<int> GetOverallPendingCountAsync()
        {
            return await _context.SmsMessages
                .CountAsync(m => m.Status == SmsStatus.Pending);
        }
    }
}