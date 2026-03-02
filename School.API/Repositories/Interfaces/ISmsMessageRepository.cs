using School.API.Data.DBModels.SMS;

namespace School.API.Repositories.Interfaces
{
    public interface ISmsMessageRepository
    {
        Task<SmsMessage> AddAsync(SmsMessage message);
        Task AddRangeAsync(IEnumerable<SmsMessage> messages);
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
    }
}
