// Services/Implementations/FeeManagement/LedgerHtmlBuilder.cs
using System.Net;
using System.Text;
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Implementations.Accounts
{
    /// <summary>
    /// Renders a Student or Family Ledger as a self-contained HTML document for
    /// PDF conversion — the server-side twin of the printed ledger.
    /// </summary>
    /// <remarks>
    /// The ledger the office prints is built in the browser
    /// (School.Web/wwwroot/js/ledgerReport.js), so there was nothing on the
    /// server to reuse. This mirrors that layout deliberately — same grey title
    /// block, black account bar, #e8ecf0 headings, #f0f4f8 totals and boxed
    /// closing balance — so a parent's WhatsApp copy and the counter's printout
    /// read as the same document. Change one and change the other.
    ///
    /// Both take their figures from SpRPT_StudentLedger / SpRPT_FamilyLedger,
    /// so the numbers cannot disagree even if the styling drifts.
    ///
    /// Inline CSS only, and no images or web fonts: PuppeteerPdfRenderer
    /// renders the string offline, and anything it had to fetch would stall the
    /// render or silently go missing.
    /// </remarks>
    public static class LedgerHtmlBuilder
    {
        private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        private static string Amount(decimal v) =>
            v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

        // Positive is owed to the school (Dr), negative is paid ahead (Cr).
        // Exactly zero carries no side, matching the printed ledger.
        private static string Balance(decimal v) =>
            Math.Abs(v) < 0.005m ? "0.00" : Amount(Math.Abs(v)) + (v > 0 ? "Dr" : "Cr");

        private static string Day(DateTime? d) => d?.ToString("dd MMM yyyy") ?? "";

        // dd MMM yyyy hh:mm tt, but the time only when the procedure supplied
        // one — it does that solely for rows whose entry happened on the line's
        // own date, so a bulk-generated fee posting shows the date alone.
        private static string DayTime(DateTime? d, string? entryTime) =>
            string.IsNullOrWhiteSpace(entryTime) ? Day(d) : $"{Day(d)} {entryTime}";

        public static string Build(
            LedgerResponseDto data,
            string title,
            string schoolName,
            string schoolAddress)
        {
            var showStudent = data.Students is { Count: > 1 };
            var span = showStudent ? 5 : 4;

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>")
              .Append(E(title)).Append(" - ").Append(E(data.AccountTitle))
              .Append("</title><style>")
              .Append(@"
    * { box-sizing: border-box; margin: 0; padding: 0;
        -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    body { font-family: Arial, Helvetica, sans-serif; font-size: 11px; color: #000; }
    @page { size: A4 landscape; margin: 8mm 10mm; }
    table { border-collapse: collapse; width: 100%; }
    th, td { font-size: 11px; }
    thead { display: table-header-group; }
    tr { page-break-inside: avoid; }
    .th   { border: 1px solid #999; padding: 4px 6px; text-align: center;
            background: #e8ecf0; vertical-align: middle; }
    .td   { border: 1px solid #ccc; padding: 3px 6px; }
    .num  { border: 1px solid #ccc; padding: 3px 6px; text-align: right; white-space: nowrap; }
    .tot  { border: 1px solid #ccc; padding: 3px 6px; text-align: right;
            font-weight: 700; background: #f0f4f8; }")
              .Append("</style></head><body>");

            // School header
            sb.Append("<div style=\"text-align:center;margin-bottom:6px;\">")
              .Append("<div style=\"font-size:15px;font-weight:700;\">").Append(E(schoolName)).Append("</div>")
              .Append("<div style=\"font-size:10px;\">").Append(E(schoolAddress)).Append("</div></div>");

            // Title block + date range
            sb.Append("<div style=\"text-align:center;margin-bottom:10px;\">")
              .Append("<div style=\"background:#d8d8d8;border:1px solid #aaa;padding:6px;")
              .Append("font-size:14px;font-weight:700;letter-spacing:.5px;\">")
              .Append(E(title)).Append("</div>")
              .Append("<div style=\"background:#e4e4e4;border:1px solid #aaa;border-top:none;")
              .Append("padding:5px;font-size:12px;\">Account Ledger From ( ")
              .Append(Day(data.DateFrom)).Append(" ) To ( ").Append(Day(data.DateTo))
              .Append(" )</div></div>");

            // Account bar
            sb.Append("<div style=\"background:#000;color:#fff;padding:5px 10px;font-size:13px;")
              .Append("font-weight:700;text-align:center;\">")
              .Append(showStudent ? "Family : " : "Student : ")
              .Append(E(data.AccountTitle));
            if (!string.IsNullOrWhiteSpace(data.AccountCode))
                sb.Append("&nbsp; &nbsp;").Append(E(data.AccountCode));
            sb.Append("</div>");

            if (!string.IsNullOrWhiteSpace(data.AccountInfo))
                sb.Append("<div style=\"border:1px solid #ccc;border-top:none;padding:3px 10px;")
                  .Append("font-size:10px;text-align:center;background:#fafafa;\">")
                  .Append(E(data.AccountInfo)).Append("</div>");

            // Ledger table
            sb.Append("<div style=\"margin-bottom:14px;\"><table><thead><tr>")
              .Append("<th class=\"th\" style=\"width:135px;\">Date</th>")
              .Append("<th class=\"th\" style=\"width:110px;\">Voucher Type</th>")
              .Append("<th class=\"th\" style=\"width:90px;\">Voucher No.</th>");
            if (showStudent) sb.Append("<th class=\"th\" style=\"width:160px;\">Student</th>");
            sb.Append("<th class=\"th\">Description</th>")
              .Append("<th class=\"th\" style=\"width:110px;\">Debit</th>")
              .Append("<th class=\"th\" style=\"width:110px;\">Credit</th>")
              .Append("<th class=\"th\" style=\"width:130px;\">Balance</th>")
              .Append("</tr></thead><tbody>");

            sb.Append("<tr><td class=\"td\" colspan=\"").Append(span)
              .Append("\" style=\"font-weight:700;background:#f0f4f8;\">Opening Balance</td>")
              .Append("<td class=\"tot\">0.00</td><td class=\"tot\">0.00</td><td class=\"tot\">")
              .Append(Balance(data.OpeningBalance)).Append("</td></tr>");

            foreach (var r in data.Rows)
            {
                sb.Append("<tr><td class=\"td\" style=\"white-space:nowrap;\">")
                  .Append(DayTime(r.LineDate, r.EntryTime)).Append("</td>")
                  .Append("<td class=\"td\">").Append(E(r.VoucherType)).Append("</td>")
                  .Append("<td class=\"td\" style=\"text-align:center;\">").Append(E(r.VNo)).Append("</td>");
                if (showStudent)
                    sb.Append("<td class=\"td\">").Append(E(r.StudentName)).Append("</td>");
                sb.Append("<td class=\"td\">").Append(E(r.Description)).Append("</td>")
                  .Append("<td class=\"num\">").Append(Amount(r.Debit)).Append("</td>")
                  .Append("<td class=\"num\">").Append(Amount(r.Credit)).Append("</td>")
                  .Append("<td class=\"num\" style=\"font-weight:600;\">").Append(Balance(r.Balance))
                  .Append("</td></tr>");
            }

            sb.Append("<tr><td class=\"tot\" colspan=\"").Append(span).Append("\">Total</td>")
              .Append("<td class=\"tot\">").Append(Amount(data.TotalDebit)).Append("</td>")
              .Append("<td class=\"tot\">").Append(Amount(data.TotalCredit)).Append("</td>")
              .Append("<td class=\"tot\">").Append(Balance(data.ClosingBalance)).Append("</td>")
              .Append("</tr></tbody></table></div>");

            // Per-child subtotals — only worth the space when there is more than one.
            if (showStudent)
            {
                sb.Append("<div style=\"margin-bottom:14px;\">")
                  .Append("<div style=\"background:#000;color:#fff;padding:5px 10px;font-size:13px;")
                  .Append("font-weight:700;text-align:center;\">Students in this Family</div>")
                  .Append("<table><thead><tr>")
                  .Append("<th class=\"th\" style=\"text-align:left;\">Student</th>")
                  .Append("<th class=\"th\" style=\"width:130px;\">Debit</th>")
                  .Append("<th class=\"th\" style=\"width:130px;\">Credit</th>")
                  .Append("<th class=\"th\" style=\"width:150px;\">Balance</th></tr></thead><tbody>");

                foreach (var s in data.Students)
                    sb.Append("<tr><td class=\"td\" style=\"font-weight:600;\">").Append(E(s.StudentName))
                      .Append("</td><td class=\"num\">").Append(Amount(s.Debit))
                      .Append("</td><td class=\"num\">").Append(Amount(s.Credit))
                      .Append("</td><td class=\"num\" style=\"font-weight:600;\">").Append(Balance(s.Balance))
                      .Append("</td></tr>");

                sb.Append("</tbody></table></div>");
            }

            sb.Append("<div style=\"text-align:right;margin-top:6px;\">")
              .Append("<span style=\"font-size:12px;font-weight:600;margin-right:8px;\">Closing Balance</span>")
              .Append("<span style=\"border:2px solid #000;padding:4px 18px;font-size:13px;font-weight:700;\">")
              .Append(Balance(data.ClosingBalance)).Append("</span></div>");

            sb.Append("<div style=\"margin-top:12px;padding-top:4px;border-top:1px solid #999;")
              .Append("display:flex;justify-content:space-between;font-size:10px;\">")
              .Append("<span>Printed On: ").Append(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"))
              .Append("</span><span>").Append(E(data.AccountTitle)).Append("</span></div>")
              .Append("<div style=\"text-align:center;font-size:9px;padding-top:4px;\">")
              .Append("EasySchool 0304-1003238</div>");

            return sb.Append("</body></html>").ToString();
        }
    }
}
