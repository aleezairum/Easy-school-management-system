using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrPayrollRun")]
    public class HrPayrollRun
    {
        [Key]
        public int Id { get; set; }

        public int PayYear { get; set; }
        public byte PayMonth { get; set; }

        /// <summary>
        /// The campus this run pays. Null is the whole school in one run.
        /// A month is run one way or the other - see PayrollService.GenerateAsync.
        /// </summary>
        public int? CampusId { get; set; }

        public HrPayrollRunStatus Status { get; set; } = HrPayrollRunStatus.Draft;

        public int? GeneratedBy { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public int? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
    }
}
