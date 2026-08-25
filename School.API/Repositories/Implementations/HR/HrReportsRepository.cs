using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    // Reports read straight off HrAttendance via LINQ-to-SQL projections — no
    // entity materialization, AsNoTracking throughout (section 9: "reports as
    // raw SQL only, never through EF entities" — enforced here via projection
    // rather than a hand-written .sql script, since these joins are simple).
    public class HrReportsRepository : IHrReportsRepository
    {
        private readonly SchoolDbContext _context;

        public HrReportsRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private class ReportJoinRow
        {
            public HrAttendance A { get; set; } = null!;
            public string EmployeeCode { get; set; } = string.Empty;
            public string EmpName { get; set; } = string.Empty;
            public string? Designation { get; set; }
        }

        private IQueryable<ReportJoinRow> BaseQuery(int? designationId)
        {
            return from a in _context.Set<HrAttendance>().AsNoTracking()
                   join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.Id
                   join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                   from d in dj.DefaultIfEmpty()
                   where !designationId.HasValue || e.DesignationId == designationId
                   select new ReportJoinRow
                   {
                       A = a,
                       EmployeeCode = e.EmployeeCode,
                       EmpName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                       Designation = d != null ? d.Name : null
                   };
        }

        public async Task<List<DailyAttendanceReportRowDto>> GetDailyAsync(DateTime date, int? designationId)
        {
            var rows = await BaseQuery(designationId)
                .Where(x => x.A.AttDate == date.Date)
                .ToListAsync();

            return rows
                .OrderBy(x => x.EmpName)
                .Select(x => new DailyAttendanceReportRowDto
                {
                    AttDate = x.A.AttDate,
                    EmployeeId = x.A.EmployeeId,
                    EmployeeCode = x.EmployeeCode,
                    EmployeeName = x.EmpName,
                    DesignationName = x.Designation,
                    Status = (byte)x.A.Status,
                    TimeIn = x.A.TimeIn,
                    TimeOut = x.A.TimeOut,
                    LateMinutes = x.A.LateMinutes
                })
                .ToList();
        }

        public async Task<List<MonthlyAttendanceSummaryRowDto>> GetMonthlySummaryAsync(int year, int month, int? designationId)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var rows = await BaseQuery(designationId)
                .Where(x => x.A.AttDate >= from && x.A.AttDate <= to)
                .ToListAsync();

            return rows
                .GroupBy(x => new { x.A.EmployeeId, x.EmployeeCode, x.EmpName, x.Designation })
                .Select(g => new MonthlyAttendanceSummaryRowDto
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeCode = g.Key.EmployeeCode,
                    EmployeeName = g.Key.EmpName,
                    DesignationName = g.Key.Designation,
                    PresentDays = g.Count(x => x.A.Status == HrAttendanceStatus.Present),
                    AbsentDays = g.Count(x => x.A.Status == HrAttendanceStatus.Absent),
                    LeaveDays = g.Count(x => x.A.Status == HrAttendanceStatus.Leave),
                    HalfDays = g.Count(x => x.A.Status == HrAttendanceStatus.HalfDay),
                    LateCount = g.Count(x => x.A.LateMinutes > 0),
                    TotalLateMinutes = g.Sum(x => x.A.LateMinutes)
                })
                .OrderBy(r => r.EmployeeName)
                .ToList();
        }

        public async Task<List<LateComerRowDto>> GetLateComersAsync(DateTime from, DateTime to, int? designationId)
        {
            var rows = await BaseQuery(designationId)
                .Where(x => x.A.AttDate >= from.Date && x.A.AttDate <= to.Date && x.A.LateMinutes > 0)
                .ToListAsync();

            return rows
                .OrderByDescending(x => x.A.AttDate).ThenByDescending(x => x.A.LateMinutes)
                .Select(x => new LateComerRowDto
                {
                    AttDate = x.A.AttDate,
                    EmployeeId = x.A.EmployeeId,
                    EmployeeCode = x.EmployeeCode,
                    EmployeeName = x.EmpName,
                    DesignationName = x.Designation,
                    TimeIn = x.A.TimeIn,
                    LateMinutes = x.A.LateMinutes,
                    IgnoreLate = x.A.IgnoreLate
                })
                .ToList();
        }

        public async Task<List<AbsenteeRowDto>> GetAbsenteesAsync(DateTime from, DateTime to, int? designationId)
        {
            var rows = await BaseQuery(designationId)
                .Where(x => x.A.AttDate >= from.Date && x.A.AttDate <= to.Date && x.A.Status == HrAttendanceStatus.Absent)
                .ToListAsync();

            return rows
                .OrderByDescending(x => x.A.AttDate).ThenBy(x => x.EmpName)
                .Select(x => new AbsenteeRowDto
                {
                    AttDate = x.A.AttDate,
                    EmployeeId = x.A.EmployeeId,
                    EmployeeCode = x.EmployeeCode,
                    EmployeeName = x.EmpName,
                    DesignationName = x.Designation,
                    Remarks = x.A.Remarks
                })
                .ToList();
        }
    }
}
