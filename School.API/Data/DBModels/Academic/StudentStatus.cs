using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Academic
{
    public class StudentStatus
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [StringLength(100)]
        public string VName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
