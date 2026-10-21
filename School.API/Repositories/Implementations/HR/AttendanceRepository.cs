using Microsoft.EntityFrameworkCore;
using School.API.DTOs.HR;
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

        public async Task<List<GridEmployeeRow>> GetEmployeesAsync(
            IEnumerable<int>? designationIds, int? campusId = null, string? sortBy = null,
            DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Employees.AsNoTracking().Where(e => e.IsActive);

            // Only the people actually employed over these dates. Somebody who
            // joins in April has no attendance in March — showing them on the
            // March sheet invites a row to be marked for a day they were not
            // there, and leaves them looking absent all month if it is not.
            // The same at the other end: nothing after their last day.
            if (from.HasValue)
            {
                var end = (to ?? from).Value.Date;
                query = query.Where(e => e.JoiningDate == null || e.JoiningDate.Value.Date <= end);
            }

            if (to.HasValue)
            {
                var begin = (from ?? to).Value.Date;
                query = query.Where(e => e.LeavingDate == null || e.LeavingDate.Value.Date >= begin);
            }

            // Several designations at once: the office marks the teachers, then
            // the operators and the support staff, and wants them in one sheet.
            // An empty list means no filter, the same as none at all.
            var designations = designationIds?.Where(id => id > 0).Distinct().ToList();
            if (designations is { Count: > 0 })
                query = query.Where(e => e.DesignationId != null && designations.Contains(e.DesignationId.Value));

            // Staff with no campus recorded are excluded from a campus-specific
            // sheet rather than folded into it — the same rule the Staff
            // Statement follows, so the two cannot disagree about who belongs
            // to a campus.
            if (campusId.HasValue)
                query = query.Where(e => e.CampusId == campusId.Value);

            var rows = await (
                from e in query
                join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                from d in dj.DefaultIfEmpty()
                select new GridEmployeeRow
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    DesignationId = e.DesignationId,
                    DesignationName = d != null ? d.Name : null,
                    JoiningDate = e.JoiningDate,
                    LeavingDate = e.LeavingDate,
                    OffDay1 = e.OffDay1,
                    OffDay2 = e.OffDay2
                }).ToListAsync();

            return Sort(rows, sortBy);
        }

        /// <summary>
        /// Orders the sheet. Employee code is the default, because that is the
        /// order the register is kept in and the order the office reads down.
        /// </summary>
        /// <remarks>
        /// Sorted here in memory rather than in SQL so the code can be compared
        /// NATURALLY: as text, "EMP-7" sorts after "EMP-68" and before "EMP-8",
        /// which is wrong the moment a code is not zero-padded. The list is one
        /// school's staff, so the cost of sorting it here is nothing.
        /// </remarks>
        public static List<GridEmployeeRow> Sort(List<GridEmployeeRow> rows, string? sortBy) =>
            (sortBy ?? "code").Trim().ToLowerInvariant() switch
            {
                "name" => rows.OrderBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
                              .ThenBy(r => CodeKey(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                              .ToList(),

                // Everyone of one designation together, each group in code order.
                // Staff with no designation go last rather than first, where an
                // empty name would put them.
                "designation" => rows.OrderBy(r => string.IsNullOrWhiteSpace(r.DesignationName) ? 1 : 0)
                                     .ThenBy(r => r.DesignationName, StringComparer.OrdinalIgnoreCase)
                                     .ThenBy(r => CodeKey(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                                     .ThenBy(r => CodeKey(r.EmployeeCode).Number)
                                     .ToList(),

                // "code", and anything unrecognised — an unknown value must not
                // leave the sheet in whatever order the database happened to give.
                _ => rows.OrderBy(r => CodeKey(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(r => CodeKey(r.EmployeeCode).Number)
                         .ThenBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
                         .ToList()
            };

        /// <summary>
        /// Splits a code into its letters and its number — "EMP-68" becomes
        /// ("EMP-", 68) — so codes sort the way a person reads them.
        /// </summary>
        /// <remarks>
        /// Public because the Monthly Amounts sheet sorts by the same rule. Two
        /// screens listing the same staff must list them in the same order, and
        /// a second copy of this would drift from the first.
        /// </remarks>
        public static (string Text, long Number) CodeKey(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return (string.Empty, 0);

            var i = code.Length;
            while (i > 0 && char.IsDigit(code[i - 1])) i--;

            var digits = code[i..];
            return (code[..i], digits.Length > 0 && long.TryParse(digits, out var n) ? n : 0);
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

        public async Task UpsertAsync(
            List<HrAttendance> rows,
            HashSet<(int EmployeeId, DateTime AttDate)>? dutySwapSpecified = null)
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
                    current.TimeIn = row.TimeIn;
                    current.TimeOut = row.TimeOut;
                    current.Status = row.Status;
                    current.LateMinutes = row.LateMinutes;
                    current.EarlyMinutes = row.EarlyMinutes;
                    current.WorkedMinutes = row.WorkedMinutes;
                    current.LeaveTypeId = row.LeaveTypeId;
                    current.IgnoreLate = row.IgnoreLate;
                    current.Remarks = row.Remarks;

                    // Only when this save was about them — see the interface.
                    if (dutySwapSpecified?.Contains((row.EmployeeId, row.AttDate)) == true)
                    {
                        current.ShortMinutes = row.ShortMinutes;
                        current.ExtraMinutes = row.ExtraMinutes;
                    }

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

        /// <summary>
        /// Replaces a day's trips out of school with exactly what was sent, and
        /// writes the day's total back onto the attendance row.
        /// </summary>
        /// <remarks>
        /// Replace rather than merge: the screen sends the whole list it is
        /// showing, so a trip the office deleted has to disappear. The minutes
        /// are worked out here and never taken from the client — a browser that
        /// sent 600 minutes for a ten-minute trip would otherwise be charged
        /// straight to someone's salary.
        /// </remarks>
        public async Task ReplaceOutingsAsync(List<SaveAttendanceCellDto> cells, int? userId)
        {
            if (cells.Count == 0) return;

            var employeeIds = cells.Select(c => c.EmployeeId).Distinct().ToList();
            var dates = cells.Select(c => c.AttDate.Date).Distinct().ToList();

            var attendance = await _context.Set<HrAttendance>()
                .Where(a => employeeIds.Contains(a.EmployeeId) && dates.Contains(a.AttDate))
                .ToListAsync();
            var byKey = attendance.ToDictionary(a => (a.EmployeeId, a.AttDate));

            var attendanceIds = attendance.Select(a => a.Id).ToList();
            var existing = await _context.HrAttendanceOutings
                .Where(o => attendanceIds.Contains(o.AttendanceId))
                .ToListAsync();

            foreach (var cell in cells)
            {
                if (cell.Outings is null) continue;
                if (!byKey.TryGetValue((cell.EmployeeId, cell.AttDate.Date), out var day)) continue;

                var old = existing.Where(o => o.AttendanceId == day.Id).ToList();
                if (old.Count > 0) _context.HrAttendanceOutings.RemoveRange(old);

                var total = 0;
                foreach (var outing in cell.Outings)
                {
                    var minutes = MinutesBetween(outing.OutTime, outing.InTime);
                    total += minutes;

                    _context.HrAttendanceOutings.Add(new HrAttendanceOuting
                    {
                        AttendanceId = day.Id,
                        OutTime = outing.OutTime,
                        InTime = outing.InTime,
                        Minutes = minutes,
                        Reason = string.IsNullOrWhiteSpace(outing.Reason) ? null : outing.Reason.Trim(),
                        CreatedBy = userId,
                        CreatedAt = DateTime.Now
                    });
                }

                day.OutingMinutes = total;
                day.UpdatedBy = userId;
                day.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Minutes between leaving and returning. Zero while they are still out
        /// (no return time yet) and zero for a return that is not after the
        /// departure — a typo must not turn into a credit or a huge charge.
        /// </summary>
        public static int MinutesBetween(TimeSpan outTime, TimeSpan? inTime)
        {
            if (!inTime.HasValue) return 0;

            var minutes = (int)(inTime.Value - outTime).TotalMinutes;
            return minutes > 0 ? minutes : 0;
        }

        public async Task<Dictionary<long, List<HrAttendanceOuting>>> GetOutingsAsync(IEnumerable<long> attendanceIds)
        {
            var ids = attendanceIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<long, List<HrAttendanceOuting>>();

            var rows = await _context.HrAttendanceOutings.AsNoTracking()
                .Where(o => ids.Contains(o.AttendanceId))
                .OrderBy(o => o.OutTime)
                .ToListAsync();

            return rows.GroupBy(o => o.AttendanceId)
                       .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
