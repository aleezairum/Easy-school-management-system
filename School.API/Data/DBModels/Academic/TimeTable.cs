using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Data.DBModels.HR;

namespace School.API.Data.DBModels.Academic
{
    [Table("TimeTables")]
    public class TimeTable
    {
        [Key]
        public int Id { get; set; }

        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SMSClass? Class { get; set; }

        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public virtual SMSSection? Section { get; set; }

        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual SMSSubject? Subject { get; set; }

        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Employee? Teacher { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AcademicSessionYear? Session { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public int PeriodNumber { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [MaxLength(50)]
        public string? RoomNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
