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

        /// <summary>Minutes out of school during the day. 0 when there were none.</summary>
        public int OutingMinutes { get; set; }

        /// <summary>Minutes of this person's duty somebody else covered.</summary>
        public int ShortMinutes { get; set; }

        /// <summary>Minutes this person worked covering for somebody else.</summary>
        public int ExtraMinutes { get; set; }

        /// <summary>The individual trips, so the office can see and edit them.</summary>
        public List<AttendanceOutingDto> Outings { get; set; } = new();

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

        /// <summary>Minutes out of school during the day. 0 when there were none.</summary>
        public int OutingMinutes { get; set; }

        /// <summary>Minutes of this person's duty somebody else covered.</summary>
        public int ShortMinutes { get; set; }

        /// <summary>Minutes this person worked covering for somebody else.</summary>
        public int ExtraMinutes { get; set; }

        /// <summary>The individual trips, so the office can see and edit them.</summary>
        public List<AttendanceOutingDto> Outings { get; set; } = new();

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

        /// <summary>
        /// The duty swap, typed by the office: minutes somebody else covered
        /// for this person, and minutes this person covered for somebody else.
        /// Null leaves whatever is on the row alone, so a save that only
        /// changes a time does not wipe them.
        /// </summary>
        public int? ShortMinutes { get; set; }
        public int? ExtraMinutes { get; set; }

        /// <summary>
        /// The day's trips out of school, as the screen currently shows them.
        /// Null means "leave the outings alone" — a save that only changes a
        /// time or a remark must not wipe them. An empty list means "this day
        /// has none", which is how the last one is removed.
        /// </summary>
        public List<AttendanceOutingDto>? Outings { get; set; }

        /// <summary>Where the row came from. Null is Manual (the screen); the machine sends Device.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public School.API.Data.DBModels.HR.HrAttendanceSource? Source { get; set; }
    }

    /// <summary>One trip out of school and back.</summary>
    public class AttendanceOutingDto
    {
        public long Id { get; set; }
        public TimeSpan OutTime { get; set; }

        /// <summary>Null while the employee is still out.</summary>
        public TimeSpan? InTime { get; set; }

        /// <summary>Worked out by the server; whatever the client sends is ignored.</summary>
        public int Minutes { get; set; }

        public string? Reason { get; set; }
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

    /// <summary>POST /api/hr/attendance/recalculate — a date range to work out again.</summary>
    public class RecalculateAttendanceDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    /// <summary>What a recalculation touched.</summary>
    public class AttendanceRecalculateResultDto
    {
        public int Rows { get; set; }
        public int Recalculated { get; set; }

        /// <summary>Days with times but no timing to judge them by.</summary>
        public int Skipped { get; set; }

        public int StatusChanged { get; set; }
        public int MinutesChanged { get; set; }
    }
}

