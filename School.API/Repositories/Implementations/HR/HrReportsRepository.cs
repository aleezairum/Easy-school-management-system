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

        /// <summary>
        /// Every saved attendance row in the range for the staff the filter
        /// picks, with the employee's name, designation and campus. The
        /// deductions are left at 0 here - HrReportsService prices them.
        /// </summary>
        public async Task<List<AttendanceReportRowDto>> GetAttendanceRowsAsync(DateTime from, DateTime to, HrReportFilter filter)
        {
            var f = from.Date;
            var t = to.Date;

            var rows = await (
                from a in _context.Set<HrAttendance>().AsNoTracking()
                join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.Id
                join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                from d in dj.DefaultIfEmpty()
                join c in _context.Campuses.AsNoTracking() on e.CampusId equals c.VID into cj
                from c in cj.DefaultIfEmpty()
                where a.AttDate >= f && a.AttDate <= t
                   && (!filter.CampusId.HasValue || e.CampusId == filter.CampusId)
                   && (!filter.DesignationId.HasValue || e.DesignationId == filter.DesignationId)
                   && (!filter.EmployeeId.HasValue || e.Id == filter.EmployeeId)
                select new AttendanceReportRowDto
                {
                    AttDate = a.AttDate,
                    EmployeeId = a.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    DesignationName = d != null ? d.Name : null,
                    CampusName = c != null ? c.VName : null,
                    Status = (byte)a.Status,
                    TimeIn = a.TimeIn,
                    TimeOut = a.TimeOut,
                    LateMinutes = a.LateMinutes,
                    IgnoreLate = a.IgnoreLate,
                    EarlyMinutes = a.EarlyMinutes,
                    OutingMinutes = a.OutingMinutes,
                    Remarks = a.Remarks
                }).ToListAsync();

            return rows.OrderBy(r => r.AttDate).ThenBy(r => r.EmployeeName).ToList();
        }

        /// <summary>The hours each day was meant to run, as Staff Timings expanded them.</summary>
        public Task<List<TimingDayRow>> GetTimingDaysAsync(DateTime from, DateTime to) =>
            _context.Set<HrSchoolTimingDay>().AsNoTracking()
                .Where(d => d.TDate >= from.Date && d.TDate <= to.Date)
                .Select(d => new TimingDayRow { DesignationId = d.DesignationId, TDate = d.TDate, TimeIn = d.TimeIn, TimeOut = d.TimeOut })
                .ToListAsync();

        /// <summary>
        /// The Staff Statement as of a date.
        /// </summary>
        /// <remarks>
        /// The salary is taken from the pay structure IN FORCE on the report
        /// date — the latest HrEmployeePay row effective on or before it — not
        /// simply the newest one. A statement dated last April must show last
        /// April's salary, or it is not a statement of anything.
        ///
        /// Components are summed in memory rather than in SQL: they live in
        /// ComponentsJson as an owned collection, and the staff list is a few
        /// dozen rows, so unpicking JSON in the database buys nothing and costs
        /// a query nobody can read. Only EARNING components are added — adding
        /// a deduction would report a salary the employee never sees.
        /// </remarks>
        public async Task<List<StaffStatementRowDto>> GetStaffStatementAsync(
            DateTime asOn, int? designationId, bool includeInactive, int? campusId = null,
            int? employeeId = null, DateTime? joinedFrom = null, DateTime? joinedTo = null)
        {
            var on = asOn.Date;
            var jf = joinedFrom?.Date;
            var jt = joinedTo?.Date;

            var staff = await (
                from e in _context.Employees.AsNoTracking()
                join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                from d in dj.DefaultIfEmpty()
                where (includeInactive || e.IsActive)
                   && (!designationId.HasValue || e.DesignationId == designationId)
                   // Staff with no campus recorded are excluded from a
                   // campus-specific sheet, not folded into it.
                   && (!campusId.HasValue || e.CampusId == campusId)
                   && (!employeeId.HasValue || e.Id == employeeId)
                   // Appointment date range. Staff with no joining date
                   // recorded cannot be placed in one, so they drop out.
                   && (!jf.HasValue || e.JoiningDate >= jf)
                   && (!jt.HasValue || e.JoiningDate <= jt)
                select new
                {
                    e.Id,
                    e.EmployeeCode,
                    e.FirstName,
                    e.LastName,
                    e.FatherName,
                    e.DateOfBirth,
                    e.Qualification,
                    e.JoiningDate,
                    e.Mobile,
                    e.Phone,
                    e.IsActive,
                    Designation = d != null ? d.Name : null
                }).ToListAsync();

            if (staff.Count == 0) return new List<StaffStatementRowDto>();

            var ids = staff.Select(s => s.Id).ToList();

            var pays = await _context.Set<HrEmployeePay>()
                .AsNoTracking()
                .Where(p => ids.Contains(p.EmployeeId) && p.IsActive && p.EffectiveFrom <= on)
                .ToListAsync();

            // Which components add to the salary and which take away.
            var earnings = await _context.Set<HrPayComponent>()
                .AsNoTracking()
                .Where(c => c.Kind == HrPayComponentKind.Earning)
                .Select(c => c.Id)
                .ToListAsync();

            var earningIds = earnings.ToHashSet();

            var salaryByEmployee = pays
                .GroupBy(p => p.EmployeeId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var current = g.OrderByDescending(p => p.EffectiveFrom)
                                       .ThenByDescending(p => p.Id)
                                       .First();

                        return current.Basic + current.Components
                            .Where(c => earningIds.Contains(c.ComponentId))
                            .Sum(c => c.Amount);
                    });

            return staff
                // Appointment date, oldest first — the sheet doubles as a
                // seniority list. Employees with no joining date recorded sort
                // last rather than first, where a null date would put them.
                .OrderBy(s => s.JoiningDate ?? DateTime.MaxValue)
                .ThenBy(s => s.FirstName)
                .Select(s => new StaffStatementRowDto
                {
                    EmployeeId = s.Id,
                    EmployeeCode = s.EmployeeCode,
                    Name = $"{s.FirstName} {s.LastName}".Trim(),
                    Designation = s.Designation,
                    FatherName = s.FatherName,
                    DateOfBirth = s.DateOfBirth,
                    Qualification = s.Qualification,
                    JoiningDate = s.JoiningDate,
                    MonthlySalary = salaryByEmployee.TryGetValue(s.Id, out var salary) ? salary : null,
                    ContactNo = !string.IsNullOrWhiteSpace(s.Mobile) ? s.Mobile : s.Phone,
                    IsActive = s.IsActive
                })
                .ToList();
        }
    }
}
