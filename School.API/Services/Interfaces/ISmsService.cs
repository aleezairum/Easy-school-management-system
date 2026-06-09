using School.API.DTOs;

namespace School.API.Services.Interfaces
{
    public interface ISmsService
    {
        // Enqueue-only: saves recipients to the DB queue with Status=Pending
        // and returns the jobId + queued count. Actual sending is done by
        // SmsQueueProcessor (background service).
        Task<SmsQueueStatusDto> EnqueueBulkAsync(SmsSendDto dto);
        Task<SmsQueueStatusDto> EnqueueFeeReminderAsync(FeeSmsSendDto dto);

        Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId);
        Task<int> GetOverallPendingCountAsync();

        Task<IEnumerable<SmsHistoryDto>> GetHistoryAsync(SmsHistoryFilterDto filter);
        Task<int> GetHistoryCountAsync(SmsHistoryFilterDto filter);
        Task<List<SmsRecipientDto>> GetAbsentStudentsAsync(DateTime date);
        Task<List<SmsRecipientDto>> GetLateStudentsAsync(DateTime date);
        Task<List<SmsRecipientDto>> GetFeeDefaultersAsync();
    }
}
