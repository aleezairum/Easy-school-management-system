using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.DTOs.Common;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;

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

        public async Task<IEnumerable<SmsMessage>> GetHistoryAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50)
        {
            return await _context
                .Set<SmsMessage>()
                .FromSqlRaw(
                    "EXEC SpGet_SmsMessages @MessageType={0}, @Status={1}, @FromDate={2}, @ToDate={3}, @Page={4}, @PageSize={5}, @CountOnly={6}",
                    messageType.HasValue ? (object)(int)messageType.Value : DBNull.Value,
                    status.HasValue ? (object)(int)status.Value : DBNull.Value,
                    fromDate ?? (object)DBNull.Value,
                    toDate ?? (object)DBNull.Value,
                    page,
                    pageSize,
                    0)
                .ToListAsync();
        }

        public async Task<int> GetHistoryCountAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var result = await _context
                .Set<SmsCountDto>()
                .FromSqlRaw(
                    "EXEC SpGet_SmsMessages @MessageType={0}, @Status={1}, @FromDate={2}, @ToDate={3}, @Page={4}, @PageSize={5}, @CountOnly={6}",
                    messageType.HasValue ? (object)(int)messageType.Value : DBNull.Value,
                    status.HasValue ? (object)(int)status.Value : DBNull.Value,
                    fromDate ?? (object)DBNull.Value,
                    toDate ?? (object)DBNull.Value,
                    1,
                    int.MaxValue,
                    1)
                .ToListAsync();

            return result.FirstOrDefault()?.TotalCount ?? 0;
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