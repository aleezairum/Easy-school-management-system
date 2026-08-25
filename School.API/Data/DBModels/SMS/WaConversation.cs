using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.SMS
{
    public enum WaDirection
    {
        Inbound = 0,
        Outbound = 1
    }

    // One chat thread per WhatsApp contact. Meta does not store conversation
    // history for us and offers no API to fetch it, so every thread is built
    // purely from webhook events we receive plus the messages we send.
    [Table("WaConversations")]
    public class WaConversation
    {
        [Key]
        public int VID { get; set; }

        // WhatsApp id: digits with country code, no '+' — e.g. 923001234567.
        [Required]
        [MaxLength(20)]
        public string WaId { get; set; } = string.Empty;

        // Name from the contact's WhatsApp profile (supplied by Meta).
        [MaxLength(200)]
        public string? ProfileName { get; set; }

        // Student/parent name resolved from our own records when the number
        // matches a student, guardian or staff row.
        [MaxLength(200)]
        public string? ContactName { get; set; }

        public DateTime? LastMessageAt { get; set; }

        [MaxLength(500)]
        public string? LastMessagePreview { get; set; }

        public WaDirection? LastMessageDirection { get; set; }

        // Meta's 24-hour customer service window runs from the contact's most
        // recent inbound message. Free-form replies are only allowed inside it;
        // after that only an approved template may be sent.
        public DateTime? LastInboundAt { get; set; }

        // Inbound messages not yet opened by school staff.
        public int UnreadCount { get; set; }

        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
