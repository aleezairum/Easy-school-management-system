using School.API.Data.DBModels.SMS;

namespace School.API.DTOs
{
    // ── Messages viewer (School.Web -> Views/SMS/Messages.cshtml) ──
    // One row of the WhatsApp message log, including the Meta template that was
    // used, the parameters sent, the fully rendered message and the outcome.
    public class SmsMessageViewDto
    {
        public int VID { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string? RecipientName { get; set; }

        public string MessageType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Short preview (MessageText) plus the complete rendered body.
        public string MessageText { get; set; } = string.Empty;
        public string? FullMessage { get; set; }

        // Meta template context
        public string? TemplateName { get; set; }
        public string? TemplateLanguage { get; set; }

        // Parameters sent to Meta, in placeholder order.
        public List<string> TemplateParams { get; set; } = new();

        // Delivery outcome
        public string? Provider { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? DeliveryStatus { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderResponse { get; set; }

        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime InsertedDate { get; set; }

        public string? JobId { get; set; }
        public bool HasAttachment { get; set; }
    }

    public class SmsMessageFilterDto
    {
        public SmsMessageType? MessageType { get; set; }
        public SmsStatus? Status { get; set; }

        // Fine-grained Meta state: accepted | sent | delivered | read | failed.
        public string? DeliveryStatus { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Free-text match on recipient phone / name.
        public string? Search { get; set; }

        public string? TemplateName { get; set; }
        public string? JobId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // Paged result + the per-status totals the viewer shows as summary tiles.
    public class SmsMessagePagedDto
    {
        public List<SmsMessageViewDto> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pending { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Held { get; set; }
        public int Rejected { get; set; }
    }
}
