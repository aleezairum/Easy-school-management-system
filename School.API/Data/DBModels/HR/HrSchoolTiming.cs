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

        /// <summary>
        /// Which designation this timing is for. NULL means every designation —
        /// the school-wide default.
        /// </summary>
        /// <remarks>
        /// A Principal, a Head Teacher and a Teacher do not keep the same hours,
        /// which is the whole reason this column exists. A row naming a
        /// designation overrides the school-wide one for that designation only,
        /// so the common case stays a single row and only the exceptions are
        /// entered. See Scripts/ReplaceShiftWithDesignationTiming.sql.
        /// </remarks>
        public int? DesignationId { get; set; }

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

        // ── How the day is judged ────────────────────────────────────────
        //
        // These came across from HrShift when it was retired. A shift was a
        // fixed pair of times and could not describe a school week; but it did
        // carry the rules for grading a day, and losing those would have
        // silently changed every attendance calculation and every hourly pay
        // component. They live here now, which also makes them per-designation.

        /// <summary>Minutes after TimeIn before someone counts as late.</summary>
        public int GraceMinutes { get; set; }

        /// <summary>
        /// Worked minutes that still count as a half day; null derives it from
        /// the day's own span (37.5%), the proportion the Shift form defaulted to.
        /// </summary>
        public int? HalfDayMinutes { get; set; }

        /// <summary>Worked minutes that count as a full day; null derives 75% of the span.</summary>
        public int? FullDayMinutes { get; set; }

        /// <summary>
        /// Hours per working day. Payroll divides by this for per-hour
        /// components (HrPayComponent.DivisorHours). Null derives it from the span.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? WorkHours { get; set; }

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
    /// A date resolves to exactly one start/end time PER DESIGNATION, which is
    /// the whole reason the range is expanded rather than scanned. Two unique
    /// indexes enforce it: one over (TDate, DesignationId) for the designation
    /// rows, and one over TDate alone for the school-wide row. They are separate
    /// because SQL Server treats NULLs as equal in a unique index, so a single
    /// index across both would allow only one school-wide row in the table.
    /// </remarks>
    [Table("HrSchoolTimingDay")]
    public class HrSchoolTimingDay
    {
        [Key]
        public int Id { get; set; }

        public int TimingId { get; set; }

        /// <summary>
        /// Copied down from the parent range so attendance can find a day by
        /// (date, designation) without joining — which is the lookup it does
        /// once per employee per day. NULL is the school-wide row.
        /// </summary>
        public int? DesignationId { get; set; }

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
