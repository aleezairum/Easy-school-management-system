using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Academic
{
    /// <summary>
    /// A class's diary for one day - homework and notes, photographed. Sent to
    /// parents on WhatsApp ("Today's Diary") for their child's class and section.
    /// </summary>
    /// <remarks>Scripts/DailyDiaryAndAnnouncements.sql creates the table.</remarks>
    [Table("SMSDailyDiary")]
    public class SMSDailyDiary
    {
        [Key]
        public int VID { get; set; }

        [Column(TypeName = "date")]
        public DateTime DiaryDate { get; set; }

        public int ClassID { get; set; }

        /// <summary>Null: every section of the class.</summary>
        public int? SectionID { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey(nameof(SMSDailyDiaryImage.DiaryID))]
        public List<SMSDailyDiaryImage> Images { get; set; } = new();
    }

    /// <summary>One page of a diary.</summary>
    [Table("SMSDailyDiaryImage")]
    public class SMSDailyDiaryImage
    {
        [Key]
        public int VID { get; set; }

        public int DiaryID { get; set; }
        public int SortOrder { get; set; }

        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(50)]
        public string ContentType { get; set; } = "image/jpeg";

        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.Now;
    }
}
