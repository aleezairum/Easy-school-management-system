// DTOs/FeeManagement/ApplyFeeDtos.cs
namespace School.API.DTOs.FeeManagement
{
    // ─── Preview Request ───────────────────────────────────────────────
    public class ApplyFeePreviewRequestDto
    {
        public int ClassID { get; set; }
        public int SectionID { get; set; }
        public int StudentID { get; set; }
        public int GradeID { get; set; }
        public int IsAvailAcademy { get; set; }
        public int FeeTypeID { get; set; }
    }

    // ─── Preview Result (mapped from SP SpGet_SMSStudentFeeApply) ──────
    public class StudentFeePreviewResultDto
    {
        public int StudentID { get; set; }
        public int ClassID { get; set; }
        public int SectionID { get; set; }
        public int GradeID { get; set; }
        public string RollNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsAvailAcademy { get; set; }
    }

    // ─── Apply Fee Request ─────────────────────────────────────────────
    public class ApplyFeeRequestDto
    {
        public string MonthFrom { get; set; } = string.Empty;  // "2026-03"
        public string MonthTo { get; set; } = string.Empty;    // "2026-05"
        public string? Description { get; set; }
        public int FeeTypeID { get; set; }
        public int SessionID { get; set; }
        public List<StudentFeeAmountDto> StudentFees { get; set; } = new();
    }

    // ─── Per-Student Fee Amount (nested in ApplyFeeRequestDto) ─────────
    public class StudentFeeAmountDto
    {
        public int StudentId { get; set; }
        public int SectionID { get; set; }
        public int GradeID { get; set; }
        public int SessionID { get; set; }
        public decimal Amount { get; set; }
    }

    // ─── Save SP Input (used internally by repository) ─────────────────
    public class SaveStudentFeeRequestDto
    {
        public int VID { get; set; } = 0;
        public DateTime VDate { get; set; }
        public int StudentID { get; set; }
        public int SessionID { get; set; }
        public int SectionID { get; set; }
        public int GradeID { get; set; }
        public int FeeTypeID { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal ClearedAmount { get; set; } = 0;
        public string? Remarks { get; set; }
        public bool IsClear { get; set; } = false;
    }

    // ─── Apply Fee Response (returned to frontend) ─────────────────────
    public class ApplyFeeResponseDto
    {
        public int TotalRequested { get; set; }
        public int TotalSaved { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalFailed { get; set; }
        public List<ApplyFeeRowResultDto> Results { get; set; } = new();
    }

    // ─── Per-Row Result (nested in ApplyFeeResponseDto) ────────────────
    public class ApplyFeeRowResultDto
    {
        public int StudentId { get; set; }
        public string Month { get; set; } = string.Empty;
        public decimal VID { get; set; }
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = string.Empty;
    }


    // ─── Get List Request ──────────────────────────────────────────
    public class GetStudentFeeRequestDto
    {
        public int? VID { get; set; }
        public int? StudentID { get; set; }
        public int? SessionID { get; set; }
        public int? SectionID { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }

    // ─── List Result (mapped from SpGet_SMSStudentFee) ─────────────
    public class StudentFeeListDto
    {
        public long VID { get; set; }
        public DateTime VDate { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;
        public string FeeTypeName { get; set; } = string.Empty;
        public decimal FeeAmount { get; set; }
        public decimal ClearedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string? Remarks { get; set; }
        public bool IsClear { get; set; }

        /// <summary>
        /// True when this fee is actually owed by the as-on date. False for a
        /// month still to come: fees are applied months ahead, and counting
        /// those as arrears is what made a September receipt announce October
        /// to December as pending.
        /// </summary>
        public bool IsDue { get; set; } = true;
    }

    /// <summary>
    /// One line in the Receive Payment screen's Find Student dropdowns — the
    /// same columns SpFillCombo_SMSStudent returns, plus what the child owes
    /// as at the voucher's date, so the clerk sees it before picking a name.
    /// </summary>
    public class PendingFeeStudentDto
    {
        public int VID { get; set; }
        public string RollNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Name_Urdu { get; set; }
        public string? ClassRollNo { get; set; }
        public int? ParentID { get; set; }
        public string? FatherName { get; set; }
        public string? FatherCNIC { get; set; }

        /// <summary>What this child owes as at the as-on date.</summary>
        public decimal PendingAmount { get; set; }

        /// <summary>How many unpaid fee rows make that up.</summary>
        public int MonthsDue { get; set; }
    }

    // ─── Report Request (for SpRPT_SMSStudentFee) ──────────────────
    public class GetStudentFeeReportRequestDto
    {
        public int? VID { get; set; }
        public int? StudentID { get; set; }
        public int? SessionID { get; set; }
        public string? SectionIDs { get; set; }   // comma separated
        public string? ClassIDs { get; set; }     // comma separated
        public string? FeeTypeIDs { get; set; }   // comma separated
        public string? CampusIDs { get; set; }    // comma separated; empty = every campus

        /// <summary>
        /// Show students whose parents have promised to pay by a date still to
        /// come. Off by default — that is the point of taking the promise — but
        /// the only way to see everything owed.
        /// </summary>
        public bool IncludePromised { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        // 0 = Paid, 1 = UnPaid, 2 = All, 3 = UnPaid for students who have LEFT
        public int ReportType { get; set; }
    }

    // ─── Report Result (mapped from SpRPT_SMSStudentFee) ───────────
    public class StudentFeeReportDto
    {
        public long VID { get; set; }
        public DateTime VDate { get; set; }
        public string? RollNo { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? ContactNo { get; set; }

        // The report groups by class and section, and orders by them. Ordering on
        // the NAME cannot work here: "Montessori", "Prep" and "New Admission"
        // carry no number at all, and "Private Students 2026-27" carries one that
        // means nothing. The ids are the school's own ordering.
        public int? ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int? SectionID { get; set; }

        public string SessionName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;

        /// <summary>Boys or Girls — from the STUDENT, since a fee row carries no campus.</summary>
        public string? CampusName { get; set; }

        /// <summary>
        /// The student's enrolment status. "Active" on the first three report
        /// types, which only ever return active students; on the dropout report
        /// it says which kind of leaver each row belongs to.
        /// </summary>
        public string? StatusName { get; set; }

        public string FeeTypeName { get; set; } = string.Empty;
        public decimal FeeAmount { get; set; }
        public decimal ClearedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string? Remarks { get; set; }
        public bool IsClear { get; set; }

        /// <summary>
        /// The date this family's promise to pay runs to, or null if they have
        /// none in force. Filled on every report type, because the procedure
        /// and this DTO must agree column for column, but it is the Unpaid
        /// report that prints it: a sheet showing a promised family has to say
        /// on its face why nobody has chased them and when the money is due.
        /// </summary>
        public DateTime? PromiseDate { get; set; }

        /// <summary>Who gave their word — father, mother, guardian.</summary>
        public string? PromisedBy { get; set; }

        /// <summary>What they said they would pay. Null means the whole amount.</summary>
        public decimal? PromisedAmount { get; set; }

        /// <summary>
        /// The child the promise was recorded against, which may be a sibling
        /// of the student on this row — a promise covers the whole family.
        /// </summary>
        public string? PromisedForName { get; set; }
    }
}