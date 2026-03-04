using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
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
    }
}