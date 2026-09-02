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

    // GET /api/hr/attendance/day — one date, every employee, for daily entry.
    public class AttendanceDayGridDto
    {
        public DateTime Date { get; set; }
        public bool IsHoliday { get; set; }
        public string? HolidayName { get; set; }

        // The school timings that apply to this date, if any are set
        // (HR > School Timings). Null means no school timing covers the date, so
        // each employee falls back to their own shift.
        public TimeSpan? SchoolTimeIn { get; set; }
        public TimeSpan? SchoolTimeOut { get; set; }

        public List<AttendanceDayRowDto> Employees { get; set; } = new();
    }

    public class AttendanceDayRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public int? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan? ShiftTimeIn { get; set; }
        public TimeSpan? ShiftTimeOut { get; set; }

        // What this employee's day actually runs on: the school timing when the
        // date has one, otherwise their shift. This is what the screen shows and
        // what "Mark all Present" fills in.
        public TimeSpan? EffectiveTimeIn { get; set; }
        public TimeSpan? EffectiveTimeOut { get; set; }

        public bool IsWeeklyOff { get; set; }

        // The saved row for this date, if there is one. Status 0 = not entered.
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
