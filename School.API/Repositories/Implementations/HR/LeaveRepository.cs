using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly SchoolDbContext _context;

        public LeaveRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveDto>> GetAllAsync(byte? status)
        {
            var query = from l in _context.Set<HrLeave>().AsNoTracking()
                        join e in _context.Employees.AsNoTracking() on l.EmployeeId equals e.Id
                        join lt in _context.Set<HrLeaveType>().AsNoTracking() on l.LeaveTypeId equals lt.Id
                        select new { l, e, lt };

            if (status.HasValue)
                query = query.Where(x => (byte)x.l.Status == status.Value);

            var result = await query
                .OrderByDescending(x => x.l.CreatedAt)
                .Select(x => new LeaveDto
                {
                    Id = x.l.Id,
                    EmployeeId = x.l.EmployeeId,
                    EmployeeName = x.e.FirstName + (x.e.LastName != null ? " " + x.e.LastName : ""),
                    LeaveTypeId = x.l.LeaveTypeId,
                    LeaveTypeName = x.lt.Name,
                    FromDate = x.l.FromDate,
                    ToDate = x.l.ToDate,
                    Days = x.l.Days,
                    Status = (byte)x.l.Status,
                    Reason = x.l.Reason,
                    ApprovedAt = x.l.ApprovedAt
                })
                .ToListAsync();

            return result;
        }

        public async Task<HrLeave> ApplyAsync(ApplyLeaveDto dto)
        {
            var days = (decimal)(dto.ToDate.Date - dto.FromDate.Date).TotalDays + 1;
            var entity = new HrLeave
            {
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,
                FromDate = dto.FromDate.Date,
                ToDate = dto.ToDate.Date,
                Days = days,
                Status = HrLeaveStatus.Pending,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<HrLeave>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrLeave?> UpdateStatusAsync(int id, byte status, int? approvedBy)
        {
            var entity = await _context.Set<HrLeave>().FirstOrDefaultAsync(l => l.Id == id);
            if (entity == null) return null;

            entity.Status = (HrLeaveStatus)status;
            entity.ApprovedBy = approvedBy;
            entity.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
