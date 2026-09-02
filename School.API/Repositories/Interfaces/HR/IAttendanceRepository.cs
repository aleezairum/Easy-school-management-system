using School.API.Data.DBModels.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public class GridEmployeeRow
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? ShiftId { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }
        public byte? ShiftOffDay1 { get; set; }
        public byte? ShiftOffDay2 { get; set; }

        // The shift carries the default weekly off days; the employee record only
        // overrides them. Setting either day on the employee replaces the shift's
        // pair outright, so a one-day override doesn't silently keep the other.
        public bool HasOwnOffDays => OffDay1.HasValue || OffDay2.HasValue;
        public byte? EffectiveOffDay1 => HasOwnOffDays ? OffDay1 : ShiftOffDay1;
        public byte? EffectiveOffDay2 => HasOwnOffDays ? OffDay2 : ShiftOffDay2;
    }

    public interface IAttendanceRepository
    {
        Task<List<GridEmployeeRow>> GetEmployeesAsync(int? designationId);
        Task<Dictionary<int, HrShift>> GetShiftsByIdsAsync(IEnumerable<int> ids);
        Task<Dictionary<(int EmployeeId, DateTime Date), HrAttendance>> GetAttendanceRowsAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to);
        Task<Dictionary<(int EmployeeId, DateTime Date), int>> GetApprovedLeaveDatesAsync(
            IEnumerable<int> employeeIds, DateTime from, DateTime to);
        Task UpsertAsync(List<HrAttendance> rows);
    }
}
