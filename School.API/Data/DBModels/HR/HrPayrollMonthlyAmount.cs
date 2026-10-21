using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// The amounts the office types by hand for one member of staff for one
    /// month — the columns the school's salary sheet has always carried.
    /// </summary>
    /// <remarks>
    /// Per month, not effective-dated like the pay structure: an incentive is
    /// a different figure every month and for every person, which is exactly
    /// why there was nowhere to record one before. See
    /// Scripts/SalarySheetColumns.sql.
    /// </remarks>
    [Table("HrPayrollMonthlyAmount")]
    public class HrPayrollMonthlyAmount
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int PayYear { get; set; }
        public byte PayMonth { get; set; }

        /// <summary>Added to the pay. No longer on the screen - Extra Duty and
        /// Academy Duty name what it was for - but still paid if a row has one.</summary>
        public decimal Incentive { get; set; }

        /// <summary>Incentive for extra duty - added to the pay.</summary>
        public decimal ExtraDuty { get; set; }

        /// <summary>Incentive for academy duty - added to the pay.</summary>
        public decimal AcademyDuty { get; set; }

        /// <summary>Added to the pay.</summary>
        public decimal Prize { get; set; }

        /// <summary>Taken off the pay. Typed as a plain (positive) amount.</summary>
        public decimal Fine { get; set; }

        /// <summary>Security money handed back — added to the pay.</summary>
        public decimal SecurityReturn { get; set; }

        /// <summary>Taken off the pay.</summary>
        public decimal PayDeduction { get; set; }

        /// <summary>Security money held back — taken off the pay.</summary>
        public decimal SecurityDeduction { get; set; }

        /// <summary>
        /// No longer used: advances are entered by date in HrStaffAdvance, and
        /// what was here was carried into it by PayrollIncentivesAndAdvance.sql.
        /// </summary>
        public decimal Advance { get; set; }

        /// <summary>Prize - Fine, kept for anything still reading the old
        /// signed column. Prize and Fine are the figures.</summary>
        public decimal PrizeFine { get; set; }

        [MaxLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
