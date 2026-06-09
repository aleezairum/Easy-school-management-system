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

        public SmsService(
            ISmsMessageRepository repository,
            SchoolDbContext context,
            ILogger<SmsService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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
        public async Task<SmsQueueStatusDto> EnqueueBulkAsync(SmsSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();
            var messages = new List<SmsMessage>(dto.Recipients.Count);

            foreach (var recipient in dto.Recipients)
            {
                var messageText = !string.IsNullOrEmpty(recipient.Message)
                    ? recipient.Message
                    : dto.Message;

                if (string.IsNullOrWhiteSpace(recipient.Phone) ||
                    string.IsNullOrWhiteSpace(messageText))
                    continue;

                messages.Add(new SmsMessage
                {
                    RecipientPhone = recipient.Phone,
                    RecipientName = recipient.Name,
                    MessageText = messageText,
                    MessageType = dto.MessageType,
                    Status = SmsStatus.Pending,
                    JobId = dto.JobId
                });
            }

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

        // ─────────────────────────────────────────────────────────────
        // Fee Reminder — ONE message per student, enqueued
        // ─────────────────────────────────────────────────────────────
        public async Task<SmsQueueStatusDto> EnqueueFeeReminderAsync(FeeSmsSendDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.JobId))
                dto.JobId = Guid.NewGuid().ToString();

            var (userId, userIp) = GetAuditContext();

            var feeRows = await GetPendingFeeRowsAsync(
                dto.StudentId, dto.SessionId, dto.ClassId, dto.SectionId);

            if (!feeRows.Any())
            {
                return new SmsQueueStatusDto { JobId = dto.JobId };
            }

            // Group by StudentId — one message per student, aggregating all fees.
            var grouped = feeRows.GroupBy(f => f.StudentId).ToList();
            var messages = new List<SmsMessage>(grouped.Count);

            foreach (var group in grouped)
            {
                var first = group.First();

                string phone = GetPreferredPhone(
                    first.IsFatherSMS, first.FatherMobile,
                    first.IsGuardianSMS, first.GuardianMobile,
                    first.IsMotherSMS, first.MotherMobile,
                    first.IsSMS, first.MobileNo);

                if (string.IsNullOrEmpty(phone)) continue;

                var feeLines = group
                    .OrderBy(f => f.VDate)
                    .Select(f => $"• {f.FeeTypeName} {f.VDate:MMMM yyyy} = Rs. {f.PendingAmount:N0}")
                    .ToList();

                decimal total = group.Sum(f => f.PendingAmount);
                string dueDate = dto.DueDate.ToString("dd-MM-yyyy");

                string text =
                    $"Dear Parents, Assalam o Alaikum!\n\n" +
                    $"Detail of Remaining Fee of *{first.StudentName}* Class *{first.ClassName}*\n\n" +
                    string.Join("\n", feeLines) + "\n" +
                    $"──────────────────\n" +
                    $"*Total Remaning Fee = Rs.{total:N0}*\n\n" +
                    $"*Please submit Total Remaining Fee upto {dueDate}.*\n\n" +
                    $"SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS) CHHONAWALA ROAD HASILPUR";

                messages.Add(new SmsMessage
                {
                    RecipientPhone = phone,
                    RecipientName = first.StudentName,
                    MessageText = text,
                    MessageType = SmsMessageType.Fee,
                    Status = SmsStatus.Pending,
                    JobId = dto.JobId
                });
            }

            if (messages.Count > 0)
                await _repository.EnqueueAsync(messages, userId, userIp);

            _logger.LogInformation(
                "Enqueued {Count} fee reminders for job {JobId}",
                messages.Count, dto.JobId);

            return new SmsQueueStatusDto
            {
                JobId = dto.JobId,
                Total = messages.Count,
                Pending = messages.Count
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Queue status — DB-backed, survives server restarts
        // ─────────────────────────────────────────────────────────────
        public Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId)
            => _repository.GetJobStatusAsync(jobId);

        public Task<int> GetOverallPendingCountAsync()
            => _repository.GetOverallPendingCountAsync();

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

                bool isSMS = reader["IsSMS"] != DBNull.Value && (bool)reader["IsSMS"];
                string mobileNo = reader["MobileNo"]?.ToString() ?? "";
                if (isSMS && !string.IsNullOrEmpty(mobileNo))
                    recipients.Add(new SmsRecipientDto { Phone = mobileNo, Name = name, Message = message });

                bool isFather = reader["IsFatherSMS"] != DBNull.Value && (bool)reader["IsFatherSMS"];
                string fatherMob = reader["FatherMobile"]?.ToString() ?? "";
                if (isFather && !string.IsNullOrEmpty(fatherMob))
                    recipients.Add(new SmsRecipientDto { Phone = fatherMob, Name = name, Message = message });

                bool isMother = reader["IsMotherSMS"] != DBNull.Value && (bool)reader["IsMotherSMS"];
                string motherMob = reader["MotherMobile"]?.ToString() ?? "";
                if (isMother && !string.IsNullOrEmpty(motherMob))
                    recipients.Add(new SmsRecipientDto { Phone = motherMob, Name = name, Message = message });

                bool isGuard = reader["IsGuardianSMS"] != DBNull.Value && (bool)reader["IsGuardianSMS"];
                string guardMob = reader["GuardianMobile"]?.ToString() ?? "";
                if (isGuard && !string.IsNullOrEmpty(guardMob))
                    recipients.Add(new SmsRecipientDto { Phone = guardMob, Name = name, Message = message });
            }

            return recipients
                .GroupBy(r => r.Phone)
                .Select(g => g.First())
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // SpGetPending_SMSStudentFee_Msg
        // ─────────────────────────────────────────────────────────────
        private async Task<List<StudentFeeDetailDto>> GetPendingFeeRowsAsync(
            int? studentId, int? sessionId, int? classId, int? sectionId)
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
