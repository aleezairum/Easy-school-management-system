using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// One trip out of school during the working day — a teacher who signs out
    /// at 11:00 and is back at 12:30.
    /// </summary>
    /// <remarks>
    /// A row per trip, not a single pair of columns on the attendance row: a
    /// person can leave twice in a day, and one pair would either lose the
    /// second trip or stretch the first over the gap between them.
    ///
    /// InTime null means they have not returned yet. Such a row is worth zero
    /// minutes until the return is entered — guessing at "now" would keep
    /// changing the day's figure every time the page was opened.
    /// </remarks>
    [Table("HrAttendanceOuting")]
    public class HrAttendanceOuting
    {
        [Key]
        public long Id { get; set; }

        public long AttendanceId { get; set; }

        /// <summary>When they left.</summary>
        public TimeSpan OutTime { get; set; }

        /// <summary>When they came back. Null while they are still out.</summary>
        public TimeSpan? InTime { get; set; }

        /// <summary>
        /// Minutes away, worked out on save from OutTime and InTime. Stored
        /// rather than derived on read so payroll and the reports always add up
        /// the same figure the office saw when it entered the times.
        /// </summary>
        public int Minutes { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
