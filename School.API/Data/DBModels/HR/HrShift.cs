using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrShift")]
    public class HrShift
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        public int GraceMinutes { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal WorkHours { get; set; }

        public int HalfDayMinutes { get; set; }
        public int FullDayMinutes { get; set; }

        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
