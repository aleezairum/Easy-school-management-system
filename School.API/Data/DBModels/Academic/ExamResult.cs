using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Models;

namespace School.API.Data.DBModels.Academic
{
    [Table("ExamResults")]
    public class ExamResult
    {
        [Key]
        public int Id { get; set; }

        public int ExamId { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }

        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? MarksObtained { get; set; }

        [MaxLength(10)]
        public string? Grade { get; set; }

        public bool IsAbsent { get; set; } = false;

        public bool IsPassed { get; set; } = false;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
