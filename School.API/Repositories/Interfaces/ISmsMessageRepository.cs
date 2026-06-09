using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces
{
    public interface ISmsMessageRepository
    {
        Task<ResponseDto> AddAsync(SmsMessage message, int userId, string userIp);
        Task AddRangeAsync(IEnumerable<SmsMessage> messages, int userId, string userIp);
        Task<IEnumerable<SmsMessage>> GetHistoryAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50);
        Task<int> GetHistoryCountAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        // ── Queue methods (used by SmsQueueProcessor) ────────────────
        Task EnqueueAsync(IEnumerable<SmsMessage> messages, int userId, string userIp);
        Task<List<SmsMessage>> GetPendingBatchAsync(int take);
        Task UpdateStatusAsync(int vid, SmsStatus status, string? errorMessage);
        Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId);
        Task<int> GetOverallPendingCountAsync();
    }
}