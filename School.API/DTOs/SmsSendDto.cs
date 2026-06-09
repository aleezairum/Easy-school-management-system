using School.API.Data.DBModels.SMS;

namespace School.API.DTOs
{
    public class SmsRecipientDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Message { get; set; } // per-recipient message
    }

    public class SmsSendDto
    {
        public List<SmsRecipientDto> Recipients { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public SmsMessageType MessageType { get; set; } = SmsMessageType.General;
        public bool UseTemplate { get; set; } = false;
        public string TemplateType { get; set; } = string.Empty;
        public string? JobId { get; set; }   // client-supplied id for live progress tracking
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

    // ── NEW: Fee Reminder Request ─────────────────────────────────
    public class FeeSmsSendDto
    {
        public DateTime DueDate { get; set; }
        public int? StudentId { get; set; }  // null = all defaulters
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? SessionId { get; set; }
        public string? JobId { get; set; }   // client-supplied id for live progress tracking
    }

    // ── NEW: Live SMS Progress Snapshot ───────────────────────────
    public class SmsProgressDto
    {
        public string JobId { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Processed => Sent + Failed;
        public bool IsComplete { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── NEW: DB-backed queue status (replaces in-memory tracker) ──
    // Returned from SmsController /progress/{jobId} and /queue-status.
    public class SmsQueueStatusDto
    {
        public string? JobId { get; set; }
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Processed => Sent + Failed;
        public bool IsComplete => Total > 0 && Pending == 0;
    }
}

namespace School.API.DTOs.SMS
{
    public class SmsStudentRawDto
    {
        public int VID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FatherMobile { get; set; }
        public string? GuardianMobile { get; set; }
        public string? MotherMobile { get; set; }
        public string? MobileNo { get; set; }
        public bool? IsFatherSMS { get; set; }
        public bool? IsGuardianSMS { get; set; }
        public bool? IsMotherSMS { get; set; }
        public bool? IsSMS { get; set; }
    }

    public class SmsStudentSpDto
    {
        public int VID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MobileNo { get; set; }
        public bool IsSMS { get; set; }
        public string? FatherMobile { get; set; }
        public bool IsFatherSMS { get; set; }
        public string? MotherMobile { get; set; }
        public bool IsMotherSMS { get; set; }
        public string? GuardianMobile { get; set; }
        public bool IsGuardianSMS { get; set; }
    }

    // ── NEW: Fee row returned from SpGetPending_SMSStudentFee ─────
    public class StudentFeeDetailDto
    {
        public DateTime VDate { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string FeeTypeName { get; set; } = "";
        public decimal FeeAmount { get; set; }
        public decimal ClearedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        // Phone fields directly from SP
        public string FatherMobile { get; set; } = "";
        public string GuardianMobile { get; set; } = "";
        public string MotherMobile { get; set; } = "";
        public string MobileNo { get; set; } = "";
        public bool IsFatherSMS { get; set; }
        public bool IsGuardianSMS { get; set; }
        public bool IsMotherSMS { get; set; }
        public bool IsSMS { get; set; }
    }
}
