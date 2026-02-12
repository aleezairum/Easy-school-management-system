using School.API.Data.DBModels.HR;

namespace School.API.DTOs
{
    public class SalaryDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DesignationName { get; set; }
        public int Month { get; set; }
        public string MonthName => GetMonthName(Month);
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal HouseRentAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal ProvidentFund { get; set; }
        public decimal LoanDeduction { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal Overtime { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public SalaryStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime? PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public DateTime CreatedAt { get; set; }

        private static string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January", 2 => "February", 3 => "March", 4 => "April",
                5 => "May", 6 => "June", 7 => "July", 8 => "August",
                9 => "September", 10 => "October", 11 => "November", 12 => "December",
                _ => "Unknown"
            };
        }
    }

    public class SalaryListDto
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DesignationName { get; set; }
        public string MonthYear { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public SalaryStatus Status { get; set; }
    }

    public class CreateSalaryDto
    {
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal HouseRentAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal ProvidentFund { get; set; }
        public decimal LoanDeduction { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal Overtime { get; set; }
        public decimal Bonus { get; set; }
        public string? Remarks { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
    }

    public class UpdateSalaryDto
    {
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal HouseRentAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal ProvidentFund { get; set; }
        public decimal LoanDeduction { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal Overtime { get; set; }
        public decimal Bonus { get; set; }
        public SalaryStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
    }

    public class GenerateSalaryDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<int>? EmployeeIds { get; set; } // null means all employees
    }

    public class ApproveSalaryDto
    {
        public List<int> SalaryIds { get; set; } = new();
    }

    public class PaySalaryDto
    {
        public int SalaryId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
    }
}
