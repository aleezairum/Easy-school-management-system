using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Models;

namespace School.API.Data.DBModels.Academic
{
    public enum AttendanceStatus
    {
        Present = 1,
        Absent = 2,
        Late = 3,
        Leave = 4,
        Holiday = 5
    }

    [Table("Attendances")]
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SMSClass? Class { get; set; }

        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public virtual SMSSection? Section { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AcademicSessionYear? Session { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        public TimeSpan? TimeIn { get; set; }

        public TimeSpan? TimeOut { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
