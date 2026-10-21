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

        /// <summary>Minutes left before the day ended, in the month.</summary>
        public int EarlyMinutes { get; set; }

        /// <summary>Minutes out of school during the day, in the month.</summary>
        public int OutingMinutes { get; set; }

        /// <summary>
        /// What one minute of late / early / out-of-school cost this month:
        /// Gross / (RateWorkingDays x RateHours x 60), as the rules worked it.
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal PerMinuteRate { get; set; }
        public decimal RateWorkingDays { get; set; }
        public decimal RateHours { get; set; }

        // ── The school's salary sheet ────────────────────────────────
        // Kept ON the payslip, not looked up when it is printed: a sheet
        // reprinted next March must show what March was paid, not what the
        // figures have been changed to since.

        /// <summary>Calendar days in the month.</summary>
        public decimal TotalDays { get; set; }

        /// <summary>Days this person was on the staff that month.</summary>
        public decimal WorkingDays { get; set; }

        /// <summary>Basic / TotalDays, the way the sheet has always worked it out.</summary>
        public decimal DailyPay { get; set; }

        public decimal Incentive { get; set; }
        public decimal ExtraDuty { get; set; }
        public decimal AcademyDuty { get; set; }
        public decimal Prize { get; set; }
        public decimal Fine { get; set; }
        public decimal SecurityReturn { get; set; }
        public decimal PayDeduction { get; set; }

        /// <summary>Prize - Fine (the old signed column, kept in step).</summary>
        public decimal PrizeFine { get; set; }

        public decimal SecurityDeduction { get; set; }
        public decimal Advance { get; set; }

        /// <summary>Extra days given for a month with no day off.</summary>
        public decimal ExtraDays { get; set; }

        /// <summary>ExtraDays x DailyPay - added to the pay.</summary>
        public decimal ExtraDaysPay { get; set; }

        /// <summary>Net pay less the advance already handed over.</summary>
        public decimal TakeHome { get; set; }

        // Materialized from/to BreakdownJson by EF (owned, ToJson).
        public List<HrPayslipLine> Breakdown { get; set; } = new();

        public DateTime? PaidDate { get; set; }

        [MaxLength(30)]
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }
    }
}
