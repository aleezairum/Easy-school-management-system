using School.API.Data.DBModels.SMS;

namespace School.API.DTOs
{
    public class SmsRecipientDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Message { get; set; } // per-recipient message

        // Values for the approved Meta template's {{1}}, {{2}}, ... placeholders,
        // in order. Populated server-side for absent/late lists and posted back
        // unchanged by the SMS page, so free-form tabs simply leave it empty.
        public List<string>? Params { get; set; }
    }

    public class SmsSendDto
    {
        public List<SmsRecipientDto> Recipients { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public SmsMessageType MessageType { get; set; } = SmsMessageType.General;
        public bool UseTemplate { get; set; } = false;
        public string TemplateType { get; set; } = string.Empty;
        public string? JobId { get; set; }   // client-supplied id for live progress tracking

        // true = bypass the free WAHA route for this batch and send through the
        // paid gateway. The SMS page sets it when the user answers "Send via
        // Paid" to the WAHA-disconnected prompt.
        public bool ForcePaid { get; set; }
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

        // Picks WHO is reminded: only students still carrying a pending due
        // dated on or before this date. Each of those students is then shown
        // ALL of their pending dues up to today, not just the ones up to
        // TillDate. Null = today (every current defaulter).
        public DateTime? TillDate { get; set; }

        public bool ForcePaid { get; set; }
    }

    // ── Result Card (Rcard) WhatsApp PDF request ──────────────────
    // One PDF result card is generated and sent to each matched student.
    public class RcardSendDto
    {
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public string Scope { get; set; } = "student"; // student | section | class | all
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? StudentId { get; set; }
        public string? Message { get; set; }           // WhatsApp caption (optional)
        public string? JobId { get; set; }
        public bool ForcePaid { get; set; }
    }

    // Send each matched student's result as a TEXT summary (no PDF attachment).
    // Same scope/filters as Rcard.
    public class ResultTextSendDto
    {
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public string Scope { get; set; } = "student"; // student | section | class | all
        public int? ClassId { get; set; }
        public int? SectionId { get; set; }
        public int? StudentId { get; set; }
        public string? Message { get; set; }           // optional prefix prepended to each result
        public string? JobId { get; set; }
        public bool ForcePaid { get; set; }
    }

    // Resolved recipient for the Rcard report: who gets the PDF and on which number.
    public class RcardRecipientDto
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public string? StudentName { get; set; }
        public string? ClassName { get; set; }   // {{2}} of the result_card template
        public string? Phone { get; set; }   // Father -> Guardian -> Student (resolved in SQL)
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

        // Parked waiting for WAHA to come back (or for the user to release them
        // through the paid gateway). A job with held rows is NOT complete — it
        // is paused, and the UI offers the two resume actions.
        public int Held { get; set; }

        // Cancelled by the user before sending. Terminal, like Sent/Failed.
        public int Rejected { get; set; }

        public int Processed => Sent + Failed + Rejected;
        public bool IsHeld => Held > 0;
        public bool IsComplete => Total > 0 && Pending == 0 && Held == 0;
    }

    // ── Preview ("Show List") ─────────────────────────────────────
    // What a send WOULD produce, without queueing anything. Lets the user see
    // the recipient count and the exact messages before committing.
    public class SmsPreviewItemDto
    {
        public int? StudentId { get; set; }
        public string? Name { get; set; }
        public string? ClassName { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Fee preview only — the total pending amount rolled into this message.
        public decimal? Amount { get; set; }
    }

    public class SmsPreviewDto
    {
        public string MessageType { get; set; } = string.Empty;

        // Messages that would actually be queued (= Items.Count).
        public int Total { get; set; }

        // Matched by the filter but dropped for want of a usable phone number.
        public int SkippedNoPhone { get; set; }

        // Which gateway this batch would use as things stand right now.
        public string Route { get; set; } = "paid";      // free | paid
        public bool FreeAvailable { get; set; }
        public string? FreeStatus { get; set; }

        public List<SmsPreviewItemDto> Items { get; set; } = new();
    }

    // Body of the resume endpoints. JobId null/empty = every held row.
    public class SmsResumeDto
    {
        public string? JobId { get; set; }
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
