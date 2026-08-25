using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly SchoolDbContext _context;

        public AttendanceRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<GridEmployeeRow>> GetEmployeesAsync(int? designationId)
        {
            var query = _context.Employees.AsNoTracking().Where(e => e.IsActive);
            if (designationId.HasValue)
                query = query.Where(e => e.DesignationId == designationId.Value);

            return await query
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new GridEmployeeRow
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    ShiftId = e.ShiftId,
                    OffDay1 = e.OffDay1,
                    OffDay2 = e.OffDay2
                })
                .ToListAsync();
        }

        public async Task<Dictionary<int, HrShift>> GetShiftsByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, HrShift>();

            return await _context.Set<HrShift>()
                .AsNoTracking()
                .Where(s => idList.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);
        }

        public async Task<Dictionary<(int EmployeeId, DateTime Date), HrAttendance>> GetAttendanceRowsAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to)
        {
            var idList = employeeIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<(int, DateTime), HrAttendance>();

            var rows = await _context.Set<HrAttendance>()
                .AsNoTracking()
                .Where(a => idList.Contains(a.EmployeeId) && a.AttDate >= from.Date && a.AttDate <= to.Date)
                .ToListAsync();

            return rows.ToDictionary(a => (a.EmployeeId, a.AttDate));
        }

        public async Task<Dictionary<(int EmployeeId, DateTime Date), int>> GetApprovedLeaveDatesAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to)
        {
            var idList = employeeIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<(int, DateTime), int>();

            var leaves = await _context.Set<HrLeave>()
                .AsNoTracking()
                .Where(l => idList.Contains(l.EmployeeId)
                            && l.Status == HrLeaveStatus.Approved
                            && l.FromDate <= to.Date && l.ToDate >= from.Date)
                .ToListAsync();

            var map = new Dictionary<(int, DateTime), int>();
            foreach (var leave in leaves)
            {
                var start = leave.FromDate.Date < from.Date ? from.Date : leave.FromDate.Date;
                var end = leave.ToDate.Date > to.Date ? to.Date : leave.ToDate.Date;
                for (var d = start; d <= end; d = d.AddDays(1))
                    map[(leave.EmployeeId, d)] = leave.LeaveTypeId;
            }
            return map;
        }

        public async Task UpsertAsync(List<HrAttendance> rows)
        {
            if (rows.Count == 0) return;

            var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
            var dates = rows.Select(r => r.AttDate).Distinct().ToList();

            var existing = await _context.Set<HrAttendance>()
                .Where(a => employeeIds.Contains(a.EmployeeId) && dates.Contains(a.AttDate))
                .ToListAsync();
            var existingMap = existing.ToDictionary(a => (a.EmployeeId, a.AttDate));

            foreach (var row in rows)
            {
                if (existingMap.TryGetValue((row.EmployeeId, row.AttDate), out var current))
                {
                    current.ShiftId = row.ShiftId;
                    current.TimeIn = row.TimeIn;
                    current.TimeOut = row.TimeOut;
                    current.Status = row.Status;
                    current.LateMinutes = row.LateMinutes;
                    current.EarlyMinutes = row.EarlyMinutes;
                    current.WorkedMinutes = row.WorkedMinutes;
                    current.LeaveTypeId = row.LeaveTypeId;
                    current.IgnoreLate = row.IgnoreLate;
                    current.Remarks = row.Remarks;
                    current.UpdatedBy = row.CreatedBy;
                    current.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.Set<HrAttendance>().Add(row);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
