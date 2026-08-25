using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.SMS
{
    // A single message in a chat thread, in either direction. Inbound rows come
    // from the Meta webhook; outbound rows are written when we send — both from
    // the chat composer and from the bulk campaign queue, so a parent's thread
    // shows the complete history.
    [Table("WaMessages")]
    public class WaMessage
    {
        [Key]
        public int VID { get; set; }

        public int ConversationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string WaId { get; set; } = string.Empty;

        public WaDirection Direction { get; set; }

        // Meta's message id ("wamid.xxx"). Inbound ids also let us de-duplicate
        // webhook retries, which Meta sends whenever it is unsure we received one.
        [MaxLength(150)]
        public string? MessageId { get; set; }

        // text | image | document | audio | video | sticker | location | template
        // Only text is composed today; the rest are recorded so an incoming
        // media message still appears in the thread with a placeholder.
        [MaxLength(30)]
        public string MessageKind { get; set; } = "text";

        // Message body, or the caption for a media message.
        public string? Body { get; set; }

        // Media reference for inbound attachments (download is a separate call).
        [MaxLength(150)]
        public string? MediaId { get; set; }

        [MaxLength(100)]
        public string? MediaMimeType { get; set; }

        [MaxLength(260)]
        public string? MediaFileName { get; set; }

        // Outbound delivery state: accepted | sent | delivered | read | failed.
        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(50)]
        public string? ErrorCode { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        // wamid this message replies to, when the contact used WhatsApp's reply.
        [MaxLength(150)]
        public string? ContextMessageId { get; set; }

        // When the message actually happened (Meta's timestamp for inbound).
        public DateTime MessageTime { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Whether school staff have opened this inbound message.
        public bool IsRead { get; set; }

        // Links a mirrored row back to the campaign message in SmsMessages.
        public int? SmsMessageVID { get; set; }

        // Template used, when this outbound row came from a campaign send.
        [MaxLength(100)]
        public string? TemplateName { get; set; }

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
