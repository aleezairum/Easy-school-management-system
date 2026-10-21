using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// The threshold for extra days: a month must have MORE working days than
    /// this. A setting rather than a constant, so the school can change it.
    /// One row, Id 1. See Scripts/ExtraDays.sql.
    /// </summary>
    [Table("HrExtraDaysSetting")]
    public class HrExtraDaysSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } = 1;

        public int MinWorkingDays { get; set; } = 20;

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// How many extra days a designation gets, remembered so the office does
    /// not type the same figures every month.
    /// </summary>
    [Table("HrExtraDaysRule")]
    public class HrExtraDaysRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DesignationId { get; set; }

        public decimal ExtraDays { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Extra days actually given to one person for one month. Payroll pays
    /// ExtraDays x the sheet's Daily Pay (Basic / days in the month).
    /// </summary>
    [Table("HrPayrollExtraDays")]
    public class HrPayrollExtraDays
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int PayYear { get; set; }
        public byte PayMonth { get; set; }
        public decimal ExtraDays { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
