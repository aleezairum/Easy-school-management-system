using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrLeaveType")]
    public class HrLeaveType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsPaid { get; set; } = true;

        [Column(TypeName = "decimal(4,2)")]
        public decimal DeductDays { get; set; }

        public int? MaxPerYear { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
