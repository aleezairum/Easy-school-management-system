// DTOs/SMS/WaBotDtos.cs
//
// Plain carriers for the WhatsApp auto-reply lookups. These are read with
// ADO.NET rather than EF — the same approach WaChatRepository already uses for
// its contact lookup — so none of them needs registering as a keyless entity.
namespace School.API.DTOs.SMS
{
    public class BotStudentDto
    {
        public int StudentId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RollNo { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? FatherName { get; set; }

        public string ClassLabel =>
            string.Join(" - ", new[] { ClassName, SectionName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public class BotDueDto
    {
        public DateTime VDate { get; set; }
        public string? FeeTypeName { get; set; }
        public decimal PendingAmount { get; set; }
    }

    public class BotVoucherDto
    {
        // The voucher header id — the bot re-renders the real A4 voucher from it.
        public decimal Vid { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public List<string> Lines { get; set; } = new();
    }

    public class BotLedgerSummaryDto
    {
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        // Signed: positive is owed to the school, negative is paid in advance.
        public decimal ClosingBalance { get; set; }
        public List<string> RecentLines { get; set; } = new();
    }

    public class BotAttendanceDto
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int Total => Present + Absent + Leave;
        public decimal Percentage => Total == 0 ? 0 : Math.Round(Present * 100m / Total, 1);
    }

    public class BotResultDto
    {
        // Needed to re-render the official result card for this exam.
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public string? SubjectName { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
    }
}
