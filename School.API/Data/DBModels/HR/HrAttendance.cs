using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrAttendance")]
    public class HrAttendance
    {
        [Key]
        public long Id { get; set; }

        public int EmployeeId { get; set; }

        [Column("AttDate")]
        public DateTime AttDate { get; set; }

        public int? ShiftId { get; set; }

        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }

        public HrAttendanceStatus Status { get; set; }

        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public int WorkedMinutes { get; set; }

        public int? LeaveTypeId { get; set; }

        public bool IgnoreLate { get; set; }

        public HrAttendanceSource Source { get; set; } = HrAttendanceSource.Manual;

        public Guid? ImportBatchId { get; set; }

        [MaxLength(300)]
        public string? Remarks { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
