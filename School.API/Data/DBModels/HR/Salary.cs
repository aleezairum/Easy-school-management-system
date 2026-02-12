using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    public enum SalaryStatus
    {
        Pending = 1,
        Approved = 2,
        Paid = 3,
        Cancelled = 4
    }

    [Table("Salaries")]
    public class Salary
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        // Allowances
        [Column(TypeName = "decimal(18,2)")]
        public decimal HouseRentAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MedicalAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TransportAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherAllowances { get; set; } = 0;

        // Deductions
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxDeduction { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProvidentFund { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LoanDeduction { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherDeductions { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Overtime { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; } = 0;

        [NotMapped]
        public decimal TotalAllowances => HouseRentAllowance + MedicalAllowance + TransportAllowance + OtherAllowances;

        [NotMapped]
        public decimal TotalDeductions => TaxDeduction + ProvidentFund + LoanDeduction + OtherDeductions;

        [NotMapped]
        public decimal GrossSalary => BasicSalary + TotalAllowances + Overtime + Bonus;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        [Required]
        public SalaryStatus Status { get; set; } = SalaryStatus.Pending;

        [Column(TypeName = "date")]
        public DateTime? PaymentDate { get; set; }

        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int WorkingDays { get; set; } = 0;

        public int PresentDays { get; set; } = 0;

        public int AbsentDays { get; set; } = 0;

        public int LeaveDays { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
