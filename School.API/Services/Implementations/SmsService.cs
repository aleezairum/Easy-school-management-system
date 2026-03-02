using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
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

        public SmsService(
            ISmsMessageRepository repository,
            ISmsSender smsSender,
            SchoolDbContext context,
            ILogger<SmsService> logger)
        {
            _repository = repository;
            _smsSender = smsSender;
            _context = context;
            _logger = logger;
        }

        public async Task<SmsSendResponseDto> SendBulkAsync(SmsSendDto dto)
        {
            var response = new SmsSendResponseDto();
            var messages = new List<SmsMessage>();

            foreach (var recipient in dto.Recipients)
            {
                var smsMessage = new SmsMessage
                {
                    RecipientPhone = recipient.Phone,
                    RecipientName = recipient.Name,
                    MessageText = dto.Message,
                    MessageType = dto.MessageType,
                    Status = SmsStatus.Pending,
                    InsertedDate = DateTime.UtcNow
                };

                try
                {
                    var success = await _smsSender.SendAsync(recipient.Phone, dto.Message);

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

            await _repository.AddRangeAsync(messages);
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
            return await GetStudentsByAttendanceStatusAsync(date, AttendanceStatus.Absent);
        }

        public async Task<List<SmsRecipientDto>> GetLateStudentsAsync(DateTime date)
        {
            return await GetStudentsByAttendanceStatusAsync(date, AttendanceStatus.Late);
        }

        public async Task<List<SmsRecipientDto>> GetFeeDefaultersAsync()
        {
            // Get active students with fee > 0 who have FatherMobile or GuardianMobile
            var students = await _context.Students
                .Where(s => s.IsActive && s.Fee > 0)
                .Where(s => (s.FatherMobile != null && s.FatherMobile != "")
                          || (s.GuardianMobile != null && s.GuardianMobile != ""))
                .AsNoTracking()
                .ToListAsync();

            return students.Select(s => new SmsRecipientDto
            {
                Phone = GetPreferredPhone(s),
                Name = s.Name
            }).Where(r => !string.IsNullOrEmpty(r.Phone)).ToList();
        }

        private async Task<List<SmsRecipientDto>> GetStudentsByAttendanceStatusAsync(DateTime date, AttendanceStatus status)
        {
            var records = await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.AttendanceDate == date.Date && a.Status == status)
                .AsNoTracking()
                .ToListAsync();

            return records
                .Where(a => a.Student != null)
                .Select(a => new SmsRecipientDto
                {
                    Phone = GetPreferredPhone(a.Student!),
                    Name = a.Student!.Name
                })
                .Where(r => !string.IsNullOrEmpty(r.Phone))
                .ToList();
        }

        private static string GetPreferredPhone(Data.DBModels.Accounts.Student student)
        {
            // Prefer father's mobile if SMS enabled, then guardian, then student
            if (student.IsFatherSMS == true && !string.IsNullOrEmpty(student.FatherMobile))
                return student.FatherMobile;
            if (student.IsGuardianSMS == true && !string.IsNullOrEmpty(student.GuardianMobile))
                return student.GuardianMobile;
            if (student.IsMotherSMS == true && !string.IsNullOrEmpty(student.MotherMobile))
                return student.MotherMobile;
            if (student.IsSMS == true && !string.IsNullOrEmpty(student.MobileNo))
                return student.MobileNo;

            // Fallback: any available number
            return student.FatherMobile ?? student.GuardianMobile ?? student.MotherMobile ?? student.MobileNo ?? "";
        }
    }
}
