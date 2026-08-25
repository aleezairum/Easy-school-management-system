using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.DTOs.Common;
using School.API.Services.Interfaces;

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

        // ── Messages viewer ──────────────────────────────────────────
        // Paged log including the Meta template, its parameters and the
        // delivery outcome. Backs Views/SMS/Messages.cshtml.
        Task<SmsMessagePagedDto> GetMessagesAsync(SmsMessageFilterDto filter);
        Task<SmsMessageViewDto?> GetMessageAsync(int vid);

        // ── Queue methods (used by SmsQueueProcessor) ────────────────
        Task EnqueueAsync(IEnumerable<SmsMessage> messages, int userId, string userIp);
        Task<List<SmsMessage>> GetPendingBatchAsync(int take);
        Task UpdateStatusAsync(int vid, SmsStatus status, string? errorMessage);

        // Records everything the provider reported for one send: message id,
        // error code, raw response, the rendered body and the parameters that
        // were actually transmitted.
        Task UpdateSendResultAsync(
            int vid,
            SmsStatus status,
            SmsSendOutcome outcome,
            string? provider,
            string? fullMessage,
            string? templateParamsJson);

        // Applies a Meta delivery webhook (sent / delivered / read / failed)
        // to the row carrying that wamid. Returns false when no row matches.
        Task<bool> ApplyDeliveryStatusAsync(
            string providerMessageId,
            string deliveryStatus,
            DateTime timestamp,
            string? errorCode,
            string? errorMessage);

        Task<SmsQueueStatusDto> GetJobStatusAsync(string jobId);
        Task<int> GetOverallPendingCountAsync();
    }
}