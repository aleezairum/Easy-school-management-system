namespace School.API.DTOs.HR
{
    // GET /api/hr/attendance/month response — one query, no per-employee round trips.
    public class AttendanceMonthGridDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<AttendanceGridDayDto> Days { get; set; } = new();
        public List<AttendanceEmployeeRowDto> Employees { get; set; } = new();
    }

    public class AttendanceGridDayDto
    {
        public int Day { get; set; }
        public bool IsHoliday { get; set; }
        public string? HolidayName { get; set; }
    }

    public class AttendanceEmployeeRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? ShiftId { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }
        public List<AttendanceCellDto> Cells { get; set; } = new();
    }

    public class AttendanceCellDto
    {
        public int Day { get; set; }
        public byte Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public int WorkedMinutes { get; set; }
        public int? LeaveTypeId { get; set; }
        public bool IgnoreLate { get; set; }
        public string? Remarks { get; set; }
    }

    // POST /api/hr/attendance/bulk — save the edited grid (upsert).
    // Status/LateMinutes/EarlyMinutes/WorkedMinutes are computed server-side
    // by AttendanceCalculator; the caller sends the raw inputs only (D1).
    public class SaveAttendanceCellDto
    {
        public int EmployeeId { get; set; }
        public DateTime AttDate { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public byte? StatusOverride { get; set; }   // explicit Absent/Leave/etc without times
        public int? LeaveTypeId { get; set; }
        public bool IgnoreLate { get; set; }
        public string? Remarks { get; set; }
    }

    public class SaveAttendanceBulkDto
    {
        public List<SaveAttendanceCellDto> Cells { get; set; } = new();
    }

    // POST /api/hr/attendance/fill — bulk mark a status for a date range.
    public class FillAttendanceDto
    {
        public List<int> EmployeeIds { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public byte Status { get; set; }
    }
}
