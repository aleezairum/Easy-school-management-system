using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.SMS
{
    public enum SmsMessageType
    {
        Staff = 1,
        General = 2,
        Absent = 3,
        Late = 4,
        Fee = 5,
        Result = 6,
        FromTo = 7,
        Excel = 8,
        Rcard = 9
    }

    public enum SmsStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2
    }

    [Table("SmsMessages")]
    public class SmsMessage
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? RecipientName { get; set; }

        [Required]
        [MaxLength(1000)]
        public string MessageText { get; set; } = string.Empty;

        [Required]
        public SmsMessageType MessageType { get; set; }

        [Required]
        public SmsStatus Status { get; set; } = SmsStatus.Pending;

        public DateTime? SentAt { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        // Audit fields
        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
