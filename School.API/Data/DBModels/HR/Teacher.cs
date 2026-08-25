using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("SMSTeacher")]
    public class Teacher
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [StringLength(200)]
        public string VName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? FatherName { get; set; }

        public int Gender { get; set; }

        [StringLength(20)]
        public string? CNIC { get; set; }

        [StringLength(20)]
        public string? Mobile { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Qualification { get; set; }

        public int? ExperienceYears { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
