using School.API.DTOs.HR;
using School.API.Data.DBModels.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public class GridEmployeeRow
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        /// <summary>Only used to sort the sheet by designation.</summary>
        public string? DesignationName { get; set; }

        /// <summary>
        /// When they started. Attendance before this date is not theirs to
        /// have: they were not employed yet.
        /// </summary>
        public DateTime? JoiningDate { get; set; }

        /// <summary>Their last day. Null while they are still employed.</summary>
        public DateTime? LeavingDate { get; set; }

        /// <summary>True when this employee was on the staff on that date.</summary>
        public bool EmployedOn(DateTime date) =>
            (!JoiningDate.HasValue || JoiningDate.Value.Date <= date.Date) &&
            (!LeavingDate.HasValue || LeavingDate.Value.Date >= date.Date);
        /// <summary>
        /// Which timing applies to this person: the school timing entered for
        /// their designation, falling back to the school-wide one.
        /// </summary>
        public int? DesignationId { get; set; }

        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        /// <summary>
        /// The weekly off from the school timing that applies to this employee.
        /// Filled in by the caller once the timings for the period are loaded —
        /// it used to come from the employee's shift, which no longer exists.
        /// </summary>
        public byte? TimingOffDay1 { get; set; }
        public byte? TimingOffDay2 { get; set; }

        // The timing carries the default weekly off days; the employee record only
        // overrides them. Setting either day on the employee replaces the timing's
        // pair outright, so a one-day override doesn't silently keep the other.
        public bool HasOwnOffDays => OffDay1.HasValue || OffDay2.HasValue;
        public byte? EffectiveOffDay1 => HasOwnOffDays ? OffDay1 : TimingOffDay1;
        public byte? EffectiveOffDay2 => HasOwnOffDays ? OffDay2 : TimingOffDay2;
    }

    public interface IAttendanceRepository
    {
        Task<List<GridEmployeeRow>> GetEmployeesAsync(IEnumerable<int>? designationIds, int? campusId = null, string? sortBy = null, DateTime? from = null, DateTime? to = null);
        Task<Dictionary<(int EmployeeId, DateTime Date), HrAttendance>> GetAttendanceRowsAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to);
        Task<Dictionary<(int EmployeeId, DateTime Date), int>> GetApprovedLeaveDatesAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to);
        /// <param name="dutySwapSpecified">
        /// The (employee, date) pairs whose save actually carried a duty swap.
        /// Everything else on an existing row is overwritten, but Short and
        /// Extra minutes are only touched for these: the month grid has no
        /// boxes for them and its saves must not wipe what the day screen
        /// entered.
        /// </param>
        Task UpsertAsync(List<HrAttendance> rows, HashSet<(int EmployeeId, DateTime AttDate)>? dutySwapSpecified = null);

        /// <summary>Replaces each day's trips out of school and refreshes the day total.</summary>
        Task ReplaceOutingsAsync(List<SaveAttendanceCellDto> cells, int? userId);

        /// <summary>The trips out, keyed by attendance row id.</summary>
        Task<Dictionary<long, List<HrAttendanceOuting>>> GetOutingsAsync(IEnumerable<long> attendanceIds);
    }
}
