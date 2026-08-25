namespace School.API.DTOs
{
    // ── WhatsApp two-way chat (Views/SMS/Chat.cshtml) ──

    // One row in the chat list.
    public class WaConversationDto
    {
        public int VID { get; set; }
        public string WaId { get; set; } = string.Empty;

        // Name resolved from our own student/parent records, if any.
        public string? ContactName { get; set; }

        // Name from the contact's WhatsApp profile.
        public string? ProfileName { get; set; }

        // What the UI shows: our name, else the profile name, else the number.
        public string DisplayName { get; set; } = string.Empty;

        public DateTime? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }
        public string? LastMessageDirection { get; set; }
        public int UnreadCount { get; set; }

        // Free-form replies are only allowed for 24 hours after the contact's
        // last inbound message; after that only an approved template may go out.
        public DateTime? LastInboundAt { get; set; }
        public bool WindowOpen { get; set; }
        public int WindowMinutesLeft { get; set; }
    }

    public class WaMessageDto
    {
        public int VID { get; set; }
        public string Direction { get; set; } = string.Empty;   // Inbound | Outbound
        public string? MessageId { get; set; }
        public string MessageKind { get; set; } = "text";
        public string? Body { get; set; }

        public string? MediaId { get; set; }
        public string? MediaMimeType { get; set; }
        public string? MediaFileName { get; set; }

        public string? Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime MessageTime { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public string? TemplateName { get; set; }
    }

    public class WaThreadDto
    {
        public WaConversationDto? Conversation { get; set; }
        public List<WaMessageDto> Messages { get; set; } = new();

        // True when older messages remain above the returned page.
        public bool HasMore { get; set; }
    }

    public class WaReplyDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
