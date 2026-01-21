using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Academic
{
    public class SMSSection
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [StringLength(100)]
        public string VName { get; set; } = string.Empty;

        [Required]
        public int ClassID { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }

        // Navigation property
        [ForeignKey("ClassID")]
        public virtual SMSClass? Class { get; set; }
    }
}
