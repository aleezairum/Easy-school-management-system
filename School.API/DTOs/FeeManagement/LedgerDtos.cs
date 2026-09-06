// DTOs/FeeManagement/LedgerDtos.cs
namespace School.API.DTOs.FeeManagement
{
    // ─── One line of a statement of account ────────────────────────────
    // Shape is shared by SpRPT_StudentLedger and SpRPT_FamilyLedger so a
    // single keyless entity serves both reports.
    public class LedgerRowDto
    {
        // 0 = the opening balance line, 1 = a transaction.
        public int RowType { get; set; }

        public DateTime? LineDate { get; set; }

        // Time of day the row was entered ("11:00 AM"), or null when the entry
        // happened on a different day than the line is dated — fee postings are
        // generated in bulk, so a whole year of monthly rows shares one insert
        // timestamp that describes none of them.
        //
        // A pre-formatted string on purpose: InsertedDate is written with
        // GETDATE() (server local time, UTC+5), so sending it as a DateTime
        // would route it through DateTimeJsonPolicy's UTC instant path and
        // shift every time by five hours in the browser.
        public string? EntryTime { get; set; }

        public string? VoucherType { get; set; }
        public string? VNo { get; set; }
        public string? Description { get; set; }

        // Which child the line belongs to. Always the requested student on a
        // student ledger; varies line by line on a family ledger.
        public int StudentID { get; set; }
        public string? StudentName { get; set; }

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        // Running balance, signed: positive is owed (Dr), negative is paid in
        // advance (Cr). It already includes the opening balance.
        public decimal Balance { get; set; }

        // Repeated on every row by the procedures so the header survives
        // without a second round trip.
        public string? AccountTitle { get; set; }
        public string? AccountCode { get; set; }
        public string? AccountInfo { get; set; }
    }

    // ─── A ledger, ready to render ─────────────────────────────────────
    public class LedgerResponseDto
    {
        public string? AccountTitle { get; set; }
        public string? AccountCode { get; set; }
        public string? AccountInfo { get; set; }

        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }

        // Transactions only — the opening balance is lifted out above.
        public List<LedgerRowDto> Rows { get; set; } = new();

        // Family ledger only: per-child subtotals, so a parent can see which
        // child the balance belongs to without reading every line.
        public List<LedgerStudentSummaryDto> Students { get; set; } = new();
    }

    public class LedgerStudentSummaryDto
    {
        public int StudentID { get; set; }
        public string? StudentName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    // ─── Family picker ─────────────────────────────────────────────────
    public class FamilyLookupDto
    {
        public int ParentID { get; set; }
        public string? FatherName { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherMobile { get; set; }
        public int StudentCount { get; set; }
        public string? StudentNames { get; set; }
    }
}
