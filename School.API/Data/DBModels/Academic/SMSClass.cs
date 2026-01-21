using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Academic
{
    public class SMSClass
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

        // Navigation properties
        public virtual ICollection<SMSSection> Sections { get; set; } = new List<SMSSection>();
        public virtual ICollection<SMSSubject> Subjects { get; set; } = new List<SMSSubject>();
    }
}
