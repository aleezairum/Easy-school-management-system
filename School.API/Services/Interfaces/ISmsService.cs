using School.API.DTOs;

namespace School.API.Services.Interfaces
{
    public interface ISmsService
    {
        Task<SmsSendResponseDto> SendBulkAsync(SmsSendDto dto);
        Task<IEnumerable<SmsHistoryDto>> GetHistoryAsync(SmsHistoryFilterDto filter);
        Task<int> GetHistoryCountAsync(SmsHistoryFilterDto filter);
        Task<List<SmsRecipientDto>> GetAbsentStudentsAsync(DateTime date);
        Task<List<SmsRecipientDto>> GetLateStudentsAsync(DateTime date);
        Task<List<SmsRecipientDto>> GetFeeDefaultersAsync();
    }
}
