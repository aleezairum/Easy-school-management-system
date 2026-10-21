using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.SMS
{
    /// <summary>
    /// A notice for the whole school, given on the WhatsApp menu between
    /// StartDate and EndDate.
    /// </summary>
    /// <remarks>Scripts/DailyDiaryAndAnnouncements.sql creates the table.</remarks>
    [Table("SMSAnnouncement")]
    public class SMSAnnouncement
    {
        [Key]
        public int VID { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        /// <summary>Last day it is shown. Null: until it is switched off.</summary>
        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        /// <summary>0 everyone, 1 parents only, 2 staff only.</summary>
        public byte Audience { get; set; }

        /// <summary>Null: both campuses.</summary>
        public int? CampusID { get; set; }

        [StringLength(200)]
        public string? ImageFileName { get; set; }

        [StringLength(50)]
        public string? ImageContentType { get; set; }

        public byte[]? ImageData { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
