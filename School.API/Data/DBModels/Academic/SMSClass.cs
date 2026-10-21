using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Academic
{
    [Table("SMSClass")]
    public class SMSClass
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [StringLength(100)]
        public string VName { get; set; } = string.Empty;

        /// <summary>
        /// The class name in Urdu, for the documents parents receive — the
        /// School Timing notice prints "نہم جونیئر", not "9 Junior".
        /// </summary>
        /// <remarks>
        /// Optional. Anything printing Urdu falls back to its own wording and
        /// then to <see cref="VName"/>, so a class nobody has translated shows
        /// up in English rather than blank.
        /// </remarks>
        [Column("VName_Urdu")]
        [StringLength(200)]
        public string? VNameUrdu { get; set; }

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
