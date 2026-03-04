using School.API.Data.DBModels.SMS;

namespace School.API.DTOs
{
    public class SmsRecipientDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Message { get; set; } // -- per-recipient message --
    }
    public class SmsSendDto
    {
        public List<SmsRecipientDto> Recipients { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public SmsMessageType MessageType { get; set; } = SmsMessageType.General;

        // ?? New: if true, service will auto-generate message per student ??
        public bool UseTemplate { get; set; } = false;
        public string TemplateType { get; set; } = string.Empty; // 'Absent', 'Late', 'FeeDefaulter'
    }

    public class SmsSendResultDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class SmsSendResponseDto
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public List<SmsSendResultDto> Results { get; set; } = new();
    }

    public class SmsHistoryDto
    {
        public int VID { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime InsertedDate { get; set; }
    }

    public class SmsHistoryFilterDto
    {
        public SmsMessageType? MessageType { get; set; }
        public SmsStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
