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
    }
}