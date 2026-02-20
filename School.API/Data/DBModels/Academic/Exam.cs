using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Academic
{
    public enum ExamType
    {
        Monthly = 1,
        MidTerm = 2,
        Final = 3,
        Quiz = 4,
        Assignment = 5,
        Practical = 6
    }

    public enum ExamStatus
    {
        Scheduled = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    [Table("Exams")]
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExamName { get; set; } = string.Empty;

        [Required]
        public ExamType ExamType { get; set; }

        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SMSClass? Class { get; set; }

        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual SMSSubject? Subject { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AcademicSessionYear? Session { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ExamDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalMarks { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PassingMarks { get; set; }

        [Required]
        public ExamStatus Status { get; set; } = ExamStatus.Scheduled;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? RoomNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ExamResult>? ExamResults { get; set; }
    }
}
