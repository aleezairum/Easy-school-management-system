namespace School.API.DTOs.HR
{
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public decimal DeductDays { get; set; }
        public int? MaxPerYear { get; set; }
        public bool IsActive { get; set; }
    }
}
