namespace School.API.DTOs.HR
{
    public class EmployeePayComponentAmountDto
    {
        public int ComponentId { get; set; }
        public string? ComponentName { get; set; }
        public decimal Amount { get; set; }
    }

    public class EmployeePayDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal Basic { get; set; }
        public List<EmployeePayComponentAmountDto> Components { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class SaveEmployeePayDto
    {
        public int EmployeeId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal Basic { get; set; }
        public List<EmployeePayComponentAmountDto> Components { get; set; } = new();
    }
}
