using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Enqueues SMS messages into the database queue. Actual delivery is
    // handled by SmsQueueProcessor (BackgroundService) which polls the
    // SmsMessages table for Status=Pending rows and sends them with a
    // randomized delay. This service never calls ISmsSender directly.
    public class SmsService : ISmsService
    {
        private readonly ISmsMessageRepository _repository;
        private readonly SchoolDbContext _context;
        private readonly ILogger<SmsService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Repositories.Interfaces.Academic.ISMSExamResultRepository _examResultRepository;
        private readonly ISmsTemplateCatalog _templateCatalog;
        private readonly ISmsRoutingPolicy _routingPolicy;

        public SmsService(
            ISmsMessageRepository repository,
            SchoolDbContext context,
            ILogger<SmsService> logger,
            IHttpContextAccessor httpContextAccessor,
            Repositories.Interfaces.Academic.ISMSExamResultRepository examResultRepository,
            ISmsTemplateCatalog templateCatalog,
            ISmsRoutingPolicy routingPolicy)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _examResultRepository = examResultRepository;
            _templateCatalog = templateCatalog;
            _routingPolicy = routingPolicy;
        }

        // What a send resolved to, before anything is written. Enqueue* persists
        // it; Preview* renders it. Both go through the same Prepare* method so
        // the list the user approves is the list that actually goes out.
        private sealed class PreparedBatch
        {
            public List<SmsMessage> Messages { get; } = new();
            public List<SmsPreviewItemDto> Items { get; } = new();
            public int SkippedNoPhone { get; set; }

            public void Add(SmsMessage message, SmsPreviewItemDto item)
            {
                Messages.Add(message);
                Items.Add(item);
            }
        }

        // Turns a prepared batch into the preview payload, stamping which
        // gateway it would use as things stand right now.
        private async Task<SmsPreviewDto> ToPreviewAsync(
            PreparedBatch batch, SmsMessageType messageType, bool forcePaid)
        {
            var route = await _routingPolicy.DecideAsync(messageType, forcePaid);
            var status = await _routingPolicy.GetFreeStatusAsync();

            return new SmsPreviewDto
            {
                MessageType = messageType.ToString(),
                Total = batch.Items.Count,
                SkippedNoPhone = batch.SkippedNoPhone,
                // Hold means it wanted the free route and cannot have it — the
                // page turns that into the reconnect/pay prompt.
                Route = route == SmsRoute.Free ? "free" : route == SmsRoute.Hold ? "hold" : "paid",
                FreeAvailable = status.Connected,
                FreeStatus = status.Status,
                Items = batch.Items
            };
        }

        // Stamps the approved Meta template, its parameters and the fully
        // rendered body onto a queued row. When no template is configured for
        // this message type the row stays exactly as before and goes out as
        // free-form text through whichever gateway is registered.
        private void ApplyTemplate(
            SmsMessage message, SmsMessageType messageType,
            IEnumerable<string>? parameters, string? fullMessage)
        {
            if (!string.IsNullOrWhiteSpace(fullMessage))
                message.FullMessage = fullMessage;

            var template = _templateCatalog.Resolve(messageType);
            if (template is null) return;

            message.TemplateName = template.Name;
            message.TemplateLanguage = template.Language;

            var values = parameters?.Select(p => p ?? string.Empty).ToList() ?? new List<string>();
            message.TemplateParams = JsonSerializer.Serialize(values);
        }

        // MessageText is capped at 1000 chars by the schema; the untruncated
        // body always lives in FullMessage.
        private static string Preview(string text)
            => text.Length <= 1000 ? text : text.Substring(0, 1000);

        // A class is always shown with its section, e.g. "1 G2". Some sources
        // (SpGet_StudentsForSms) already return the two joined; others return
        // them separately, so this joins whatever is supplied.
        private static string ClassWithSection(string? className, string? sectionName)
        {
            var parts = new[] { className, sectionName }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim());

            return string.Join(" ", parts);
        }

        private (int userId, string userIp) GetAuditContext()
        {
            int userId = 1; // later from JWT
            string userIp = "unknown";
            try
            {
                userIp = _httpContextAccessor.HttpContext?
                            .Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            catch { /* background context — no HttpContext */ }
            return (userId, userIp);
        }

        // ─────────────────────────────────────────────────────────────
        // Bulk Enqueue (General / Absent / Late / Excel / Staff etc.)
        // ─────────────────────────────────────────────────────────────
        private PreparedBatch PrepareBulk(SmsSendDto dto)
        {
            var batch = new PreparedBatch();

            foreach (var recipient in dto.Recipients)
            {
                var messageText = !string.IsNullOrEmpty(recipient.Message)
                    ? recipient.Message
                    : dto.Message;

                if (string.IsNullOrWhiteSpace(recipient.Phone))
                {
                    batch.SkippedNoPhone++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(messageText))
                    continue;

                var message = new SmsMessage
                {
                    RecipientPhone = recipient.Phone,
                    RecipientName = recipient.Name,
                    MessageText = Preview(messageText),
                    MessageType = dto.MessageType,
                    Status = SmsStatus.Pending,
                    JobId = dto.JobId,
                    ForcePaid = dto.ForcePaid
                };

                // Absent/late lists arrive with their placeholder values already
                // resolved server-side; free-form tabs post none.
                ApplyTemplate(message, dto.MessageType, recipient.Params, messageText);

                batch.Add(message, new SmsPreviewItemDto
                {
                    Name = recipient.Name,
                    Phone = recipient.Phone,
                    Message = messageText
                });
            }

            return batch;
        }

        public async Task<SmsQueueStatusDto> EnqueueBulkAsync(SmsSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();
            var messages = PrepareBulk(dto).Messages;

            if (messages.Count > 0)
                await _repository.EnqueueAsync(messages, userId, userIp);

            _logger.LogInformation(
                "Enqueued {Count} SMS for job {JobId} (type {Type})",
                messages.Count, dto.JobId, dto.MessageType);

            return new SmsQueueStatusDto
            {
                JobId = dto.JobId,
                Total = messages.Count,
                Pending = messages.Count
            };
        }

        public Task<SmsPreviewDto> PreviewBulkAsync(SmsSendDto dto)
            => ToPreviewAsync(PrepareBulk(dto), dto.MessageType, dto.ForcePaid);

        // ─────────────────────────────────────────────────────────────
        // Result Card (Rcard) — ONE PDF per student, enqueued. The queue
        // processor regenerates each PDF at send time (see SmsQueueProcessor)
        // and delivers it as a WhatsApp document.
        // ─────────────────────────────────────────────────────────────
        private async Task<PreparedBatch> PrepareRcardAsync(RcardSendDto dto)
        {
            // Resolve scope -> filter ids (0 = no filter).
            int classId   = (dto.Scope is "class" or "section" or "student") ? (dto.ClassId   ?? 0) : 0;
            int sectionId = (dto.Scope is "section" or "student")            ? (dto.SectionId ?? 0) : 0;
            int studentId = (dto.Scope == "student")                         ? (dto.StudentId ?? 0) : 0;

            var recipients = await _examResultRepository
                .GetRcardRecipientsAsync(dto.ExamId, classId, sectionId, studentId);

            var caption = string.IsNullOrWhiteSpace(dto.Message)
                ? "Dear Parent, please find attached your child's result card."
                : dto.Message;

            var batch = new PreparedBatch();

            foreach (var r in recipients)
            {
                if (string.IsNullOrWhiteSpace(r.Phone)) { batch.SkippedNoPhone++; continue; }

                var message = new SmsMessage
                {
                    RecipientPhone = r.Phone!,
                    RecipientName  = r.StudentName,
                    MessageText    = Preview(caption),
                    MessageType    = SmsMessageType.Rcard,
                    Status         = SmsStatus.Pending,
                    JobId          = dto.JobId,
                    ForcePaid      = dto.ForcePaid,
                    RefExamId      = dto.ExamId,
                    RefExamName    = dto.ExamName,
                    RefStudentId   = r.StudentId,
                    RefClassId     = r.ClassId,
                    RefSectionId   = r.SectionId
                };

                // result_card: {{1}} Student Name, {{2}} Class.
                // The PDF itself is generated at send time and attached as the
                // template's document header; the body placeholders are known now.
                ApplyTemplate(
                    message, SmsMessageType.Rcard,
                    new[] { r.StudentName ?? string.Empty, r.ClassName ?? string.Empty },
                    caption);

                batch.Add(message, new SmsPreviewItemDto
                {
                    StudentId = r.StudentId,
                    Name      = r.StudentName,
                    ClassName = r.ClassName,
                    Phone     = r.Phone!,
                    // The PDF is rendered at send time, so the preview shows the
                    // caption that will accompany it.
                    Message   = caption
                });
            }

            return batch;
        }

        public async Task<SmsQueueStatusDto> EnqueueRcardAsync(RcardSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();
            var batch = await PrepareRcardAsync(dto);
            var messages = batch.Messages;

            if (messages.Count > 0)
                await _repository.EnqueueAsync(messages, userId, userIp);

            _logger.LogInformation(
                "Enqueued {Count} result-card PDFs for job {JobId} (exam {ExamId}, scope {Scope}); {Skipped} skipped (no phone)",
                messages.Count, dto.JobId, dto.ExamId, dto.Scope, batch.SkippedNoPhone);

            return new SmsQueueStatusDto
            {
                JobId   = dto.JobId,
                Total   = messages.Count,
                Pending = messages.Count
            };
        }

        public async Task<SmsPreviewDto> PreviewRcardAsync(RcardSendDto dto)
            => await ToPreviewAsync(await PrepareRcardAsync(dto), SmsMessageType.Rcard, dto.ForcePaid);

        // ─────────────────────────────────────────────────────────────
        // Result as TEXT — ONE message per student, enqueued. The queue
        // processor composes each student's result summary at send time and
        // delivers it as a plain WhatsApp text (works without WAHA Plus).
        // ─────────────────────────────────────────────────────────────
        private async Task<PreparedBatch> PrepareResultTextAsync(ResultTextSendDto dto)
        {
            int classId   = (dto.Scope is "class" or "section" or "student") ? (dto.ClassId   ?? 0) : 0;
            int sectionId = (dto.Scope is "section" or "student")            ? (dto.SectionId ?? 0) : 0;
            int studentId = (dto.Scope == "student")                         ? (dto.StudentId ?? 0) : 0;

            var recipients = await _examResultRepository
                .GetRcardRecipientsAsync(dto.ExamId, classId, sectionId, studentId);

            var batch = new PreparedBatch();

            foreach (var r in recipients)
            {
                if (string.IsNullOrWhiteSpace(r.Phone)) { batch.SkippedNoPhone++; continue; }

                var message = new SmsMessage
                {
                    RecipientPhone = r.Phone!,
                    RecipientName  = r.StudentName,
                    // MessageText holds the optional prefix; the processor appends
                    // the composed result body at send time.
                    MessageText    = Preview(dto.Message ?? string.Empty),
                    MessageType    = SmsMessageType.Result,
                    Status         = SmsStatus.Pending,
                    JobId          = dto.JobId,
                    ForcePaid      = dto.ForcePaid,
                    RefExamId      = dto.ExamId,
                    RefExamName    = dto.ExamName,
                    RefStudentId   = r.StudentId,
                    RefClassId     = r.ClassId,
                    RefSectionId   = r.SectionId
                };

                // Marks/percentage are not known until the result is composed at
                // send time, so only the template identity is stamped here — the
                // queue processor fills TemplateParams and FullMessage.
                ApplyTemplate(message, SmsMessageType.Result, parameters: null, fullMessage: null);

                batch.Add(message, new SmsPreviewItemDto
                {
                    StudentId = r.StudentId,
                    Name      = r.StudentName,
                    ClassName = r.ClassName,
                    Phone     = r.Phone!,
                    // The result summary is composed at send time from live
                    // marks, so the preview shows the prefix plus a placeholder.
                    Message = string.IsNullOrWhiteSpace(dto.Message)
                        ? "(result summary composed at send time)"
                        : dto.Message + "\n(result summary composed at send time)"
                });
            }

            return batch;
        }

        public async Task<SmsQueueStatusDto> EnqueueResultTextAsync(ResultTextSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();
            var batch = await PrepareResultTextAsync(dto);
            var messages = batch.Messages;

            if (messages.Count > 0)
                await _repository.EnqueueAsync(messages, userId, userIp);

            _logger.LogInformation(
                "Enqueued {Count} result texts for job {JobId} (exam {ExamId}, scope {Scope}); {Skipped} skipped (no phone)",
                messages.Count, dto.JobId, dto.ExamId, dto.Scope, batch.SkippedNoPhone);

            return new SmsQueueStatusDto
            {
                JobId   = dto.JobId,
                Total   = messages.Count,
                Pending = messages.Count
            };
        }

        public async Task<SmsPreviewDto> PreviewResultTextAsync(ResultTextSendDto dto)
            => await ToPreviewAsync(await PrepareResultTextAsync(dto), SmsMessageType.Result, dto.ForcePaid);

        // ─────────────────────────────────────────────────────────────
        // Fee Reminder — ONE message per student, enqueued
        // ─────────────────────────────────────────────────────────────
        private async Task<PreparedBatch> PrepareFeeReminderAsync(FeeSmsSendDto dto)
        {
            var batch = new PreparedBatch();

            var feeRows = await GetPendingFeeRowsAsync(
                dto.StudentId, dto.SessionId, dto.ClassId, dto.SectionId, dto.TillDate);

            if (!feeRows.Any()) return batch;

            // Group by StudentId — one message per student, aggregating all fees.
            var grouped = feeRows.GroupBy(f => f.StudentId).ToList();

            foreach (var group in grouped)
            {
                var first = group.First();

                string phone = GetPreferredPhone(
                    first.IsFatherSMS, first.FatherMobile,
                    first.IsGuardianSMS, first.GuardianMobile,
                    first.IsMotherSMS, first.MotherMobile,
                    first.IsSMS, first.MobileNo);

                if (string.IsNullOrEmpty(phone)) { batch.SkippedNoPhone++; continue; }

                var feeLines = group
                    .OrderBy(f => f.VDate)
                    .Select(f => $"• {f.FeeTypeName} {f.VDate:MMMM yyyy} = Rs. {f.PendingAmount:N0}")
                    .ToList();

                decimal total = group.Sum(f => f.PendingAmount);
                string dueDate = dto.DueDate.ToString("dd-MMM-yyyy");
                string classWithSection = ClassWithSection(first.ClassName, first.SectionName);

                string text =
                    $"Dear Parents, Assalam o Alaikum!\n\n" +
                    $"Detail of Remaining Fee of *{first.StudentName}* Class *{classWithSection}*\n\n" +
                    string.Join("\n", feeLines) + "\n" +
                    $"──────────────────\n" +
                    $"*Total Remaning Fee = Rs.{total:N0}*\n\n" +
                    $"*Please submit Total Remaining Fee upto {dueDate}.*\n\n" +
                    $"SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS) CHHONAWALA ROAD HASILPUR";

                var message = new SmsMessage
                {
                    RecipientPhone = phone,
                    RecipientName = first.StudentName,
                    MessageText = Preview(text),
                    MessageType = SmsMessageType.Fee,
                    Status = SmsStatus.Pending,
                    JobId = dto.JobId,
                    ForcePaid = dto.ForcePaid,
                    RefStudentId = first.StudentId
                };

                // fee_reminder: {{1}} Student Name, {{2}} Class, {{3}} Fee Detail,
                // {{4}} Total, {{5}} Due Date. The template already prints
                // "Rs. {{4}}", so {{4}} carries the bare amount.
                //
                // {{3}} is flattened onto one line: WhatsApp rejects template
                // parameters containing newlines or tabs. The multi-line version
                // is still what FullMessage stores for the viewer.
                ApplyTemplate(
                    message, SmsMessageType.Fee,
                    new[]
                    {
                        first.StudentName,
                        classWithSection,
                        string.Join("  ", feeLines),
                        $"{total:N0}",
                        dueDate
                    },
                    text);

                batch.Add(message, new SmsPreviewItemDto
                {
                    StudentId = first.StudentId,
                    Name      = first.StudentName,
                    ClassName = classWithSection,
                    Phone     = phone,
                    Message   = text,
                    Amount    = total
                });
            }

            return batch;
        }

        public async Task<SmsQueueStatusDto> EnqueueFeeReminderAsync(FeeSmsSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();
            var batch = await PrepareFeeReminderAsync(dto);
            var messages = batch.Messages;

            if (messages.Count > 0)
                await _repository.EnqueueAsync(messages, userId, userIp);

            _logger.LogInformation(
                "Enqueued {Count} fee reminders for job {JobId} (till {Till}); {Skipped} skipped (no phone)",
                messages.Count, dto.JobId, dto.TillDate, batch.SkippedNoPhone);

            return new SmsQueueStatusDto
            {
                JobId = dto.JobId,
                Total = messages.Count,
                Pending = messages.Count
            };
        }

        public async Task<SmsPreviewDto> PreviewFeeReminderAsync(FeeSmsSendDto dto)
            => await ToPreviewAsync(await PrepareFeeReminderAsync(dto), SmsMessageType.Fee, dto.ForcePaid);

        // ─────────────────────────────────────────────────────────────
        // Queue status — DB-backed, survives server restarts
        // ─────────────────────────────────────────────────────────────
        public Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId)
            => _repository.GetJobStatusAsync(jobId);

        public Task<int> GetOverallPendingCountAsync()
            => _repository.GetOverallPendingCountAsync();

        // ─────────────────────────────────────────────────────────────
        // Hold / resume
        // ─────────────────────────────────────────────────────────────

        // Free resume. The caller (SmsController) is responsible for refusing
        // this while WAHA is still disconnected — otherwise the queue would
        // just park the rows again on the next tick.
        public async Task<int> ResumeHeldFreeAsync(string? jobId)
        {
            var released = await _repository.ResumeHeldAsync(jobId, forcePaid: false);
            _logger.LogInformation(
                "Resumed {Count} held message(s) on the free route (job {JobId})",
                released, jobId ?? "<all>");
            return released;
        }

        // Paid resume: stamps ForcePaid so the queue skips WAHA entirely for
        // these rows, whatever their message type is routed to.
        public async Task<int> ResumeHeldPaidAsync(string? jobId)
        {
            var released = await _repository.ResumeHeldAsync(jobId, forcePaid: true);
            _logger.LogInformation(
                "Resumed {Count} held message(s) on the paid route (job {JobId})",
                released, jobId ?? "<all>");
            return released;
        }

        public Task<int> GetHeldCountAsync(string? jobId)
            => _repository.GetHeldCountAsync(jobId);

        // ─────────────────────────────────────────────────────────────
        // Reject
        // ─────────────────────────────────────────────────────────────
        public async Task<int> RejectUnsentAsync(string? jobId)
        {
            var rejected = await _repository.RejectUnsentAsync(
                jobId, "Rejected by the user before sending.");

            _logger.LogInformation(
                "Rejected {Count} unsent message(s) (job {JobId})",
                rejected, jobId ?? "<all>");

            return rejected;
        }

        public Task<int> GetUnsentCountAsync(string? jobId)
            => _repository.GetUnsentCountAsync(jobId);

        // ─────────────────────────────────────────────────────────────
        // History
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<SmsHistoryDto>> GetHistoryAsync(SmsHistoryFilterDto filter)
        {
            var messages = await _repository.GetHistoryAsync(
                filter.MessageType, filter.Status,
                filter.FromDate, filter.ToDate,
                filter.Page, filter.PageSize);

            return messages.Select(m => new SmsHistoryDto
            {
                VID = m.VID,
                RecipientPhone = m.RecipientPhone,
                RecipientName = m.RecipientName,
                MessageText = m.MessageText,
                MessageType = m.MessageType.ToString(),
                Status = m.Status.ToString(),
                SentAt = m.SentAt,
                ErrorMessage = m.ErrorMessage,
                InsertedDate = m.InsertedDate
            });
        }

        public async Task<int> GetHistoryCountAsync(SmsHistoryFilterDto filter)
        {
            return await _repository.GetHistoryCountAsync(
                filter.MessageType, filter.Status,
                filter.FromDate, filter.ToDate);
        }

        // ─────────────────────────────────────────────────────────────
        // Messages viewer
        // ─────────────────────────────────────────────────────────────
        public Task<SmsMessagePagedDto> GetMessagesAsync(SmsMessageFilterDto filter)
            => _repository.GetMessagesAsync(filter);

        public Task<SmsMessageViewDto?> GetMessageAsync(int vid)
            => _repository.GetMessageAsync(vid);

        // ─────────────────────────────────────────────────────────────
        // Meta delivery webhook
        //
        // Meta POSTs status updates shaped as:
        //   entry[].changes[].value.statuses[] =
        //     { id: "wamid.xxx", status: "delivered", timestamp: "1699...",
        //       errors: [ { code: 131047, title: "..." } ] }
        // Each status is matched to the row carrying that wamid.
        // ─────────────────────────────────────────────────────────────
        public async Task<int> ApplyDeliveryWebhookAsync(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return 0;

            int updated = 0;

            try
            {
                using var doc = JsonDocument.Parse(rawJson);

                if (!doc.RootElement.TryGetProperty("entry", out var entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                    return 0;

                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes) ||
                        changes.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var change in changes.EnumerateArray())
                    {
                        if (!change.TryGetProperty("value", out var value) ||
                            !value.TryGetProperty("statuses", out var statuses) ||
                            statuses.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var status in statuses.EnumerateArray())
                        {
                            var messageId = status.TryGetProperty("id", out var id) ? id.GetString() : null;
                            var state = status.TryGetProperty("status", out var s) ? s.GetString() : null;

                            if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(state))
                                continue;

                            var timestamp = ParseUnixSeconds(
                                status.TryGetProperty("timestamp", out var t) ? t.GetString() : null);

                            string? errorCode = null, errorMessage = null;
                            if (status.TryGetProperty("errors", out var errors) &&
                                errors.ValueKind == JsonValueKind.Array &&
                                errors.GetArrayLength() > 0)
                            {
                                var first = errors[0];
                                if (first.TryGetProperty("code", out var c))
                                    errorCode = c.ValueKind == JsonValueKind.Number
                                        ? c.GetInt64().ToString()
                                        : c.GetString();
                                if (first.TryGetProperty("title", out var title))
                                    errorMessage = title.GetString();
                                if (first.TryGetProperty("message", out var m))
                                    errorMessage = m.GetString() ?? errorMessage;
                            }

                            if (await _repository.ApplyDeliveryStatusAsync(
                                    messageId!, state!, timestamp, errorCode, errorMessage))
                                updated++;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[Meta] Unparseable delivery webhook payload");
                return 0;
            }

            if (updated > 0)
                _logger.LogInformation("[Meta] Delivery webhook updated {Count} message(s)", updated);

            return updated;
        }

        private static DateTime ParseUnixSeconds(string? value)
            => long.TryParse(value, out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
                : DateTime.UtcNow;

        // ─────────────────────────────────────────────────────────────
        // Absent / Late / Fee Defaulters
        // ─────────────────────────────────────────────────────────────
        public async Task<List<SmsRecipientDto>> GetAbsentStudentsAsync(DateTime date)
            => await GetStudentsForSmsAsync("Absent", date);

        public async Task<List<SmsRecipientDto>> GetLateStudentsAsync(DateTime date)
            => await GetStudentsForSmsAsync("Late", date);

        public async Task<List<SmsRecipientDto>> GetFeeDefaultersAsync()
            => await GetStudentsForSmsAsync("FeeDefaulter", null);

        // ─────────────────────────────────────────────────────────────
        // SpGet_StudentsForSms
        // ─────────────────────────────────────────────────────────────
        private async Task<List<SmsRecipientDto>> GetStudentsForSmsAsync(
            string type, DateTime? date)
        {
            var recipients = new List<SmsRecipientDto>();
            var connStr = _context.Database.GetConnectionString();

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("SpGet_StudentsForSms", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@Date",
                date.HasValue ? (object)date.Value.Date : DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string name = reader["Name"]?.ToString() ?? "";
                string className = reader["ClassName"]?.ToString() ?? "";
                string message = BuildMessage(type, name, className, date);

                // absent_notification: {{1}} Student Name, {{2}} Class.
                // The SMS page posts these recipient objects back untouched, so
                // they reach EnqueueBulkAsync and end up on the queued row.
                var templateParams = new List<string> { name, className };

                bool isSMS = reader["IsSMS"] != DBNull.Value && (bool)reader["IsSMS"];
                string mobileNo = reader["MobileNo"]?.ToString() ?? "";
                if (isSMS && !string.IsNullOrEmpty(mobileNo))
                    recipients.Add(new SmsRecipientDto { Phone = mobileNo, Name = name, Message = message, Params = templateParams });

                bool isFather = reader["IsFatherSMS"] != DBNull.Value && (bool)reader["IsFatherSMS"];
                string fatherMob = reader["FatherMobile"]?.ToString() ?? "";
                if (isFather && !string.IsNullOrEmpty(fatherMob))
                    recipients.Add(new SmsRecipientDto { Phone = fatherMob, Name = name, Message = message, Params = templateParams });

                bool isMother = reader["IsMotherSMS"] != DBNull.Value && (bool)reader["IsMotherSMS"];
                string motherMob = reader["MotherMobile"]?.ToString() ?? "";
                if (isMother && !string.IsNullOrEmpty(motherMob))
                    recipients.Add(new SmsRecipientDto { Phone = motherMob, Name = name, Message = message, Params = templateParams });

                bool isGuard = reader["IsGuardianSMS"] != DBNull.Value && (bool)reader["IsGuardianSMS"];
                string guardMob = reader["GuardianMobile"]?.ToString() ?? "";
                if (isGuard && !string.IsNullOrEmpty(guardMob))
                    recipients.Add(new SmsRecipientDto { Phone = guardMob, Name = name, Message = message, Params = templateParams });
            }

            return recipients
                .GroupBy(r => r.Phone)
                .Select(g => g.First())
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // SpGetPending_SMSStudentFee_Msg
        // ─────────────────────────────────────────────────────────────
        // tillDate selects WHICH students are reminded (those still carrying a
        // due dated on or before it). The SP then returns ALL of those
        // students' pending dues up to today, so a March/April defaulter is
        // also billed for May onwards in the same message.
        private async Task<List<StudentFeeDetailDto>> GetPendingFeeRowsAsync(
            int? studentId, int? sessionId, int? classId, int? sectionId, DateTime? tillDate)
        {
            var result = new List<StudentFeeDetailDto>();
            var connStr = _context.Database.GetConnectionString();

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("SpGetPending_SMSStudentFee_Msg", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@VID", DBNull.Value);
            cmd.Parameters.AddWithValue("@StudentID",
                studentId.HasValue ? (object)studentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionID",
                sessionId.HasValue ? (object)sessionId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ClassID",
                classId.HasValue ? (object)classId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@SectionID",
                sectionId.HasValue ? (object)sectionId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TillDate",
                tillDate.HasValue ? (object)tillDate.Value.Date : DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new StudentFeeDetailDto
                {
                    StudentId = reader["StudentID"] != DBNull.Value ? Convert.ToInt32(reader["StudentID"]) : 0,
                    VDate = reader["VDate"] != DBNull.Value ? Convert.ToDateTime(reader["VDate"]) : DateTime.Now,
                    StudentName = reader["StudentName"]?.ToString() ?? "",
                    ClassName = reader["ClassName"]?.ToString() ?? "",
                    SectionName = reader["SectionName"]?.ToString() ?? "",
                    FeeTypeName = reader["FeeTypeName"]?.ToString() ?? "",
                    FeeAmount = reader["FeeAmount"] != DBNull.Value ? Convert.ToDecimal(reader["FeeAmount"]) : 0,
                    ClearedAmount = reader["ClearedAmount"] != DBNull.Value ? Convert.ToDecimal(reader["ClearedAmount"]) : 0,
                    PendingAmount = reader["PendingAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PendingAmount"]) : 0,
                    FatherMobile = reader["FatherMobile"]?.ToString() ?? "",
                    GuardianMobile = reader["GuardianMobile"]?.ToString() ?? "",
                    MotherMobile = reader["MotherMobile"]?.ToString() ?? "",
                    MobileNo = reader["MobileNo"]?.ToString() ?? "",
                    IsFatherSMS = reader["IsFatherSMS"] != DBNull.Value && (bool)reader["IsFatherSMS"],
                    IsGuardianSMS = reader["IsGuardianSMS"] != DBNull.Value && (bool)reader["IsGuardianSMS"],
                    IsMotherSMS = reader["IsMotherSMS"] != DBNull.Value && (bool)reader["IsMotherSMS"],
                    IsSMS = reader["IsSMS"] != DBNull.Value && (bool)reader["IsSMS"],
                });
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // Phone Priority Logic
        // ─────────────────────────────────────────────────────────────
        private static string GetPreferredPhone(
            bool isFather, string fatherMob,
            bool isGuard, string guardMob,
            bool isMother, string motherMob,
            bool isSMS, string mobileNo)
        {
            if (isFather && !string.IsNullOrEmpty(fatherMob)) return fatherMob;
            if (isGuard && !string.IsNullOrEmpty(guardMob)) return guardMob;
            if (isMother && !string.IsNullOrEmpty(motherMob)) return motherMob;
            if (isSMS && !string.IsNullOrEmpty(mobileNo)) return mobileNo;
            return fatherMob ?? guardMob ?? motherMob ?? mobileNo ?? "";
        }

        // ─────────────────────────────────────────────────────────────
        // Message Templates
        // ─────────────────────────────────────────────────────────────
        private static string BuildMessage(
            string type, string studentName, string className, DateTime? date = null)
        {
            return type switch
            {
                "Absent" =>
                    $"Dear Parents,\n" +
                    $"Assalamu Alaikum,\n\n" +
                    $"It is to inform you that your child, {studentName} (Class {className}), " +
                    $"is absent from school today.\n" +
                    $"Please ensure regular attendance.\n\n" +
                    $"Sir Syed Model High School (Boys & Girls)\n" +
                    $"Chhonawala Road, Hasilpur",

                "Late" =>
                    $"Dear Parents,\n" +
                    $"Assalamu Alaikum,\n\n" +
                    $"It is to inform you that your child, {studentName} (Class {className}), " +
                    $"arrived late at school today.\n" +
                    $"Please ensure punctuality.\n\n" +
                    $"Sir Syed Model High School (Boys & Girls)\n" +
                    $"Chhonawala Road, Hasilpur",

                "FeeDefaulter" =>
                    $"Dear Parents,\n" +
                    $"Assalamu Alaikum,\n\n" +
                    $"It is to inform you that your child, {studentName} (Class {className}), " +
                    $"has pending fee dues.\n" +
                    $"Please clear the dues at your earliest convenience.\n\n" +
                    $"Sir Syed Model High School (Boys & Girls)\n" +
                    $"Chhonawala Road, Hasilpur",

                _ =>
                    $"Dear Parents,\n" +
                    $"Assalamu Alaikum,\n\n" +
                    $"Information regarding your child, {studentName} (Class {className}).\n\n" +
                    $"Sir Syed Model High School (Boys & Girls)\n" +
                    $"Chhonawala Road, Hasilpur"
            };
        }
    }
}
