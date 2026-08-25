namespace School.API.DTOs.HR
{
    public class DailyAttendanceReportRowDto
    {
        public DateTime AttDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public byte Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public int LateMinutes { get; set; }
    }

    public class MonthlyAttendanceSummaryRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int HalfDays { get; set; }
        public int LateCount { get; set; }
        public int TotalLateMinutes { get; set; }
    }

    public class LateComerRowDto
    {
        public DateTime AttDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public int LateMinutes { get; set; }
        public bool IgnoreLate { get; set; }
    }

    public class AbsenteeRowDto
    {
        public DateTime AttDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? Remarks { get; set; }
    }
}
