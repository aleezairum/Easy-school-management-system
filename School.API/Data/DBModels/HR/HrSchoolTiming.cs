using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// A school-wide start/end time applied over a date range.
    /// </summary>
    /// <remarks>
    /// This is the row the screen edits as one unit. Saving it expands into
    /// <see cref="HrSchoolTimingDay"/> — one row per actual school day — and that
    /// is what attendance reads. See Scripts/HrSchoolTiming.sql.
    /// </remarks>
    [Table("HrSchoolTiming")]
    public class HrSchoolTiming
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        /// <summary>Fridays run on these instead, when both are set.</summary>
        public TimeSpan? FridayTimeIn { get; set; }
        public TimeSpan? FridayTimeOut { get; set; }

        /// <summary>Weekly off, 1 = Sunday .. 7 = Saturday. These weekdays get no day row.</summary>
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual ICollection<HrSchoolTimingDay> Days { get; set; } = new List<HrSchoolTimingDay>();
    }

    /// <summary>
    /// One school day carrying the timing that applies to it.
    /// </summary>
    /// <remarks>
    /// TDate is unique: a date resolves to exactly one start/end time, which is
    /// the whole reason the range is expanded rather than scanned.
    /// </remarks>
    [Table("HrSchoolTimingDay")]
    public class HrSchoolTimingDay
    {
        [Key]
        public int Id { get; set; }

        public int TimingId { get; set; }

        [Column("TDate")]
        public DateTime TDate { get; set; }

        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        /// <summary>
        /// This one day was edited by hand, so re-saving the range leaves it alone.
        /// </summary>
        public bool IsOverridden { get; set; }

        [ForeignKey("TimingId")]
        public virtual HrSchoolTiming? Timing { get; set; }
    }
}
