using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly ISmsMessageRepository _repository;
        private readonly ISmsSender _smsSender;
        private readonly SchoolDbContext _context;
        private readonly ILogger<SmsService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmsService(
            ISmsMessageRepository repository,
            ISmsSender smsSender,
            SchoolDbContext context,
            ILogger<SmsService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _smsSender = smsSender;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SmsSendResponseDto> SendBulkAsync(SmsSendDto dto)
        {
            int userId = 1; // later from JWT
            string userIp = _httpContextAccessor.HttpContext?
                                .Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var response = new SmsSendResponseDto();
            var messages = new List<SmsMessage>();

            foreach (var recipient in dto.Recipients)
            {
                var messageText = !string.IsNullOrEmpty(recipient.Message)
                    ? recipient.Message
                    : dto.Message;

                var smsMessage = new SmsMessage
                {
                    RecipientPhone = recipient.Phone,
                    RecipientName = recipient.Name,
                    MessageText = messageText,
                    MessageType = dto.MessageType,
                    Status = SmsStatus.Pending,
                    InsertedDate = DateTime.UtcNow
                };

                try
                {
                    var success = await _smsSender.SendAsync(recipient.Phone, messageText);

                    if (success)
                    {
                        smsMessage.Status = SmsStatus.Sent;
                        smsMessage.SentAt = DateTime.UtcNow;
                        response.TotalSent++;
                    }
                    else
                    {
                        smsMessage.Status = SmsStatus.Failed;
                        smsMessage.ErrorMessage = "Send returned false";
                        response.TotalFailed++;
                    }

                    response.Results.Add(new SmsSendResultDto
                    {
                        Phone = recipient.Phone,
                        Name = recipient.Name,
                        Success = success
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SMS to {Phone}", recipient.Phone);
                    smsMessage.Status = SmsStatus.Failed;
                    smsMessage.ErrorMessage = ex.Message;
                    response.TotalFailed++;
                    response.Results.Add(new SmsSendResultDto
                    {
                        Phone = recipient.Phone,
                        Name = recipient.Name,
                        Success = false,
                        Error = ex.Message
                    });
                }

                messages.Add(smsMessage);
            }

            await _repository.AddRangeAsync(messages, userId, userIp);
            return response;
        }

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

        public async Task<List<SmsRecipientDto>> GetAbsentStudentsAsync(DateTime date)
        {
            return await GetStudentsForSmsAsync("Absent", date);
        }

        public async Task<List<SmsRecipientDto>> GetLateStudentsAsync(DateTime date)
        {
            return await GetStudentsForSmsAsync("Late", date);
        }

        public async Task<List<SmsRecipientDto>> GetFeeDefaultersAsync()
        {
            return await GetStudentsForSmsAsync("FeeDefaulter", null);
        }

        //  Pure ADO.NET — SP call directly 
        private async Task<List<SmsRecipientDto>> GetStudentsForSmsAsync(
            string type,
            DateTime? date)
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

                // Student
                bool isSMS = reader["IsSMS"] != DBNull.Value && (bool)reader["IsSMS"];
                string mobileNo = reader["MobileNo"]?.ToString() ?? "";
                if (isSMS && !string.IsNullOrEmpty(mobileNo))
                    recipients.Add(new SmsRecipientDto
                    {
                        Phone = mobileNo,
                        Name = name,
                        Message = message
                    });

                // Father
                bool isFatherSMS = reader["IsFatherSMS"] != DBNull.Value && (bool)reader["IsFatherSMS"];
                string fatherMobile = reader["FatherMobile"]?.ToString() ?? "";
                if (isFatherSMS && !string.IsNullOrEmpty(fatherMobile))
                    recipients.Add(new SmsRecipientDto
                    {
                        Phone = fatherMobile,
                        Name = name,
                        Message = message
                    });

                // Mother
                bool isMotherSMS = reader["IsMotherSMS"] != DBNull.Value && (bool)reader["IsMotherSMS"];
                string motherMobile = reader["MotherMobile"]?.ToString() ?? "";
                if (isMotherSMS && !string.IsNullOrEmpty(motherMobile))
                    recipients.Add(new SmsRecipientDto
                    {
                        Phone = motherMobile,
                        Name = name,
                        Message = message
                    });

                // Guardian
                bool isGuardianSMS = reader["IsGuardianSMS"] != DBNull.Value && (bool)reader["IsGuardianSMS"];
                string guardianMobile = reader["GuardianMobile"]?.ToString() ?? "";
                if (isGuardianSMS && !string.IsNullOrEmpty(guardianMobile))
                    recipients.Add(new SmsRecipientDto
                    {
                        Phone = guardianMobile,
                        Name = name,
                        Message = message
                    });
            }

            // Remove duplicate phone numbers
            return recipients
                .GroupBy(r => r.Phone)
                .Select(g => g.First())
                .ToList();
        }

        //  Message templates — no header 
        private static string BuildMessage(
            string type,
            string studentName,
            string className,
            DateTime? date = null)
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