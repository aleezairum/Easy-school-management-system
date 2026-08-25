namespace School.API.DTOs.HR
{
    public class LeaveDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Days { get; set; }
        public byte Status { get; set; }
        public string? Reason { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class ApplyLeaveDto
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
    }

    public class LeaveStatusDto
    {
        public byte Status { get; set; }   // 2=Approved 3=Rejected
    }
}
