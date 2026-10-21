using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// One advance handed to a member of staff, on the date it was handed over
    /// (Staff &amp; Payroll -> Staff Advances). A month's payroll takes off the
    /// advances dated in that month. See Scripts/PayrollIncentivesAndAdvance.sql.
    /// </summary>
    [Table("HrStaffAdvance")]
    public class HrStaffAdvance
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Column(TypeName = "date")]
        public DateTime AdvanceDate { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
