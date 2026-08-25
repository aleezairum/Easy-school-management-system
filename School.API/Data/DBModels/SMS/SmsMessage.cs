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

        // Groups a batch of enqueued messages so the UI can poll progress
        // per job. Nullable for legacy rows inserted before the queue.
        [MaxLength(100)]
        public string? JobId { get; set; }

        // ── Result-card (Rcard) attachment context ──
        // For MessageType=Rcard the queue processor regenerates the student's
        // result card PDF at send time using these refs (keeps the DB small and
        // honors throttling). Null for normal text messages.
        public int? RefExamId { get; set; }
        public int? RefStudentId { get; set; }
        public int? RefClassId { get; set; }
        public int? RefSectionId { get; set; }

        [MaxLength(200)]
        public string? RefExamName { get; set; }

        // ── Meta WhatsApp Cloud API (template) context ──
        // Business-initiated WhatsApp messages must use a pre-approved template
        // instead of free-form text. These columns are all nullable so existing
        // rows and the legacy providers (WAHA/Wasender/Arsal) keep working
        // exactly as before — they simply leave them NULL.

        // Approved template name registered with Meta, e.g. "student_absent".
        [MaxLength(100)]
        public string? TemplateName { get; set; }

        // Template language code as approved, e.g. "en" / "en_US" / "ur".
        [MaxLength(10)]
        public string? TemplateLanguage { get; set; }

        // JSON array of the body parameters ({{1}}, {{2}}, ...) sent to Meta,
        // in order. Stored so the exact payload can be audited/re-sent.
        public string? TemplateParams { get; set; }

        // The complete rendered message as the parent actually receives it
        // (template body with parameters substituted). MessageText is capped at
        // 1000 chars for backwards compatibility, so long result texts live here.
        public string? FullMessage { get; set; }

        // Which sender delivered this row: Meta | Waha | Wasender | Arsal | Log.
        [MaxLength(30)]
        public string? Provider { get; set; }

        // Meta's message id ("wamid.xxx"), used to correlate delivery webhooks.
        [MaxLength(150)]
        public string? ProviderMessageId { get; set; }

        // Fine-grained delivery state from Meta: accepted | sent | delivered |
        // read | failed. Status (above) stays the coarse queue state.
        [MaxLength(20)]
        public string? DeliveryStatus { get; set; }

        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Meta error code (e.g. "132000" wrong parameter count) for diagnosis.
        [MaxLength(50)]
        public string? ErrorCode { get; set; }

        // Raw JSON returned by the provider — invaluable when a send fails.
        public string? ProviderResponse { get; set; }

        // Audit fields
        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
