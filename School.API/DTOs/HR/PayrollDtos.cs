namespace School.API.DTOs.HR
{
    public class PayrollRunDto
    {
        public int Id { get; set; }
        public int PayYear { get; set; }
        public byte PayMonth { get; set; }
        public byte Status { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalNetPay { get; set; }
    }

    public class GeneratePayrollRunDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class PayslipLineDto
    {
        public int ComponentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Kind { get; set; }
        public decimal Amount { get; set; }
    }

    public class PayslipDto
    {
        public int Id { get; set; }
        public int RunId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public decimal Basic { get; set; }
        public decimal GrossEarning { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal NetPay { get; set; }
        public decimal PresentDays { get; set; }
        public decimal AbsentDays { get; set; }
        public decimal LeaveDays { get; set; }
        public int LateMinutes { get; set; }
        public List<PayslipLineDto> Breakdown { get; set; } = new();
        public DateTime? PaidDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Reference { get; set; }
    }

    public class PayEmployeeDto
    {
        public DateTime PaidDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    public class PayRunDto
    {
        public DateTime PaidDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    public class PayrollWarningDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PayrollRunResultDto
    {
        public PayrollRunDto Run { get; set; } = new();
        public List<PayrollWarningDto> Warnings { get; set; } = new();
    }

    public class SalarySheetRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public decimal Basic { get; set; }
        public decimal GrossEarning { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal NetPay { get; set; }
        public List<PayslipLineDto> Breakdown { get; set; } = new();
    }
}
