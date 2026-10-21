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


        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }

        public HrAttendanceStatus Status { get; set; }

        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public int WorkedMinutes { get; set; }

        /// <summary>
        /// Total minutes spent out of school during the day — the sum of this
        /// day's HrAttendanceOuting rows.
        /// </summary>
        /// <remarks>
        /// Does NOT change the day's status. Someone who arrived at the bell
        /// and left at the bell was present; the hours away are charged through
        /// the SHORTLEAVE payroll component instead of being turned into a half
        /// day, which is what the school asked for.
        /// </remarks>
        public int OutingMinutes { get; set; }

        /// <summary>
        /// Minutes of this person's duty that somebody else covered.
        /// </summary>
        /// <remarks>
        /// The two sides of a duty swap. When one teacher takes another's class
        /// for an hour, the one who was covered has 60 short minutes and the one
        /// who covered has 60 extra. Payroll charges (short - extra) at the same
        /// per-minute rate as lateness, so the account settles between them: the
        /// covered teacher pays for the hour and the covering one is paid for
        /// it. Entered by the office; nothing computes these.
        /// </remarks>
        public int ShortMinutes { get; set; }

        /// <summary>Minutes this person worked covering somebody else's duty.</summary>
        public int ExtraMinutes { get; set; }

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
