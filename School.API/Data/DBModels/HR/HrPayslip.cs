using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    // One earning/deduction line on a payslip, including zero-value lines
    // where the component's ShowOnPayslip = true.
    // Owned by HrPayslip, stored as JSON in BreakdownJson (D3).
    public class HrPayslipLine
    {
        public int ComponentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public HrPayComponentKind Kind { get; set; }
        public decimal Amount { get; set; }
    }

    [Table("HrPayslip")]
    public class HrPayslip
    {
        [Key]
        public int Id { get; set; }

        public int RunId { get; set; }
        public int EmployeeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Basic { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossEarning { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDeduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetPay { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PresentDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AbsentDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal LeaveDays { get; set; }

        public int LateMinutes { get; set; }

        // Materialized from/to BreakdownJson by EF (owned, ToJson).
        public List<HrPayslipLine> Breakdown { get; set; } = new();

        public DateTime? PaidDate { get; set; }

        [MaxLength(30)]
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }
    }
}
