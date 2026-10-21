namespace School.API.DTOs.HR
{
    /// <summary>The filters every HR report takes. Null means "everyone".</summary>
    public class HrReportFilter
    {
        public int? CampusId { get; set; }
        public int? DesignationId { get; set; }
        public int? EmployeeId { get; set; }
    }

    /// <summary>
    /// One employee on one day - the Daily, Late, Early, Out of School and
    /// Absent reports all list these, each picking the rows it is about.
    /// The deductions are what payroll charges for that day's minutes.
    /// </summary>
    public class AttendanceReportRowDto
    {
        public DateTime AttDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }
        public byte Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }

        public int LateMinutes { get; set; }

        /// <summary>Lateness excused by the office: shown, never charged.</summary>
        public bool IgnoreLate { get; set; }
        public int EarlyMinutes { get; set; }
        public int OutingMinutes { get; set; }

        public decimal LateDeduction { get; set; }
        public decimal EarlyDeduction { get; set; }
        public decimal OutingDeduction { get; set; }
        public decimal TotalDeduction => LateDeduction + EarlyDeduction + OutingDeduction;

        /// <summary>
        /// No salary structure for the month, so nothing can be priced - and
        /// payroll skips the employee too. The deductions are 0 for that
        /// reason, not because the minutes are free.
        /// </summary>
        public bool NoSalaryRecord { get; set; }

        public string? Remarks { get; set; }
    }

    /// <summary>One employee over the whole date range.</summary>
    public class AttendanceSummaryRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HalfDays { get; set; }
        public int ShortLeaveDays { get; set; }
        public int ExemptDays { get; set; }

        public int LateCount { get; set; }
        public int LateMinutes { get; set; }
        public decimal LateDeduction { get; set; }

        public int EarlyCount { get; set; }
        public int EarlyMinutes { get; set; }
        public decimal EarlyDeduction { get; set; }

        public int OutingCount { get; set; }
        public int OutingMinutes { get; set; }
        public decimal OutingDeduction { get; set; }

        public decimal TotalDeduction => LateDeduction + EarlyDeduction + OutingDeduction;

        /// <summary>Some or all of the range could not be priced: no salary structure.</summary>
        public bool NoSalaryRecord { get; set; }
    }

    /// <summary>A staff member's attendance card: every day of the range, marked or not.</summary>
    public class AttendanceCardDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<AttendanceCardDayDto> Days { get; set; } = new();

        /// <summary>The marked days added up, with deductions.</summary>
        public AttendanceSummaryRowDto Totals { get; set; } = new();
        public int HolidayDays { get; set; }
        public int OffDays { get; set; }
        public int NotMarkedDays { get; set; }
    }

    public class AttendanceCardDayDto
    {
        public DateTime Date { get; set; }

        /// <summary>The saved status, 0 when the day has no attendance row.</summary>
        public byte Status { get; set; }

        /// <summary>What the card prints: Present, Holiday, Weekly Off, Not marked...</summary>
        public string Label { get; set; } = string.Empty;
        public string? HolidayName { get; set; }

        /// <summary>The hours the day was meant to run, from Staff Timings.</summary>
        public TimeSpan? DutyIn { get; set; }
        public TimeSpan? DutyOut { get; set; }

        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public int LateMinutes { get; set; }
        public bool IgnoreLate { get; set; }
        public int EarlyMinutes { get; set; }
        public int OutingMinutes { get; set; }
        public decimal LateDeduction { get; set; }
        public decimal EarlyDeduction { get; set; }
        public decimal OutingDeduction { get; set; }
        public decimal TotalDeduction => LateDeduction + EarlyDeduction + OutingDeduction;
        public string? Remarks { get; set; }
    }

    /// <summary>A day's hours for a designation (null = the whole school).</summary>
    public class TimingDayRow
    {
        public int? DesignationId { get; set; }
        public DateTime TDate { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
    }
}
