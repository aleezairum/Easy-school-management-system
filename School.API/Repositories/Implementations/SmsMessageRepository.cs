using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
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

        public async Task<SmsMessage> AddAsync(SmsMessage message)
        {
            _context.SmsMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task AddRangeAsync(IEnumerable<SmsMessage> messages)
        {
            _context.SmsMessages.AddRange(messages);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SmsMessage>> GetHistoryAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.SmsMessages.AsQueryable();

            if (messageType.HasValue)
                query = query.Where(m => m.MessageType == messageType.Value);
            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);
            if (fromDate.HasValue)
                query = query.Where(m => m.InsertedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(m => m.InsertedDate <= toDate.Value);

            return await query
                .OrderByDescending(m => m.InsertedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetHistoryCountAsync(
            SmsMessageType? messageType = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.SmsMessages.AsQueryable();

            if (messageType.HasValue)
                query = query.Where(m => m.MessageType == messageType.Value);
            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);
            if (fromDate.HasValue)
                query = query.Where(m => m.InsertedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(m => m.InsertedDate <= toDate.Value);

            return await query.CountAsync();
        }
    }
}
