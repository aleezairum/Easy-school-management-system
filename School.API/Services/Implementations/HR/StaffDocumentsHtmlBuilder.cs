using System.Globalization;
using System.Net;
using System.Text;
using School.API.Data.DBModels.HR;
using School.API.DTOs.Academic;
using School.API.DTOs.HR;

namespace School.API.Services.Implementations.HR
{
    /// <summary>
    /// The staff documents the WhatsApp bot sends as PDF: the monthly
    /// attendance card, the salary slip, and a teacher's term result.
    /// </summary>
    /// <remarks>
    /// The office prints the first two in the browser
    /// (School.Web/wwwroot/js/attendanceCardPrint.js and salarySlipPrint.js),
    /// so there was nothing on the server to reuse. These mirror those layouts
    /// - same boxes, same headings, same order - so a member of staff holding
    /// the WhatsApp copy against the office printout sees the same document.
    /// Change one and change the other.
    ///
    /// Inline CSS only, no images or web fonts: PuppeteerPdfRenderer renders
    /// the string offline.
    /// </remarks>
    public static class StaffDocumentsHtmlBuilder
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        private static string Money(decimal v) => v.ToString("N2", Inv);
        private static string MoneyOrBlank(decimal v) => v == 0 ? "" : Money(v);
        private static string Time(TimeSpan? t) => t.HasValue ? t.Value.ToString(@"hh\:mm", Inv) : "";
        private static string Day(DateTime? d) => d?.ToString("dd-MMM-yyyy", Inv) ?? "-";
        private static string Num(decimal v) => v.ToString("0.##", Inv);

        private const string BaseCss = @"
            * { box-sizing: border-box; }
            body { font-family: Arial, Helvetica, sans-serif; color: #000; margin: 0; font-size: 11px;
                   -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            table { width: 100%; border-collapse: collapse; }
            th, td { border: 1px solid #000; padding: 3px 5px; vertical-align: middle; }
            th { background: #e8e8e8; font-weight: 700; }
            .head { text-align: center; margin-bottom: 6px; }
            .head .n { font-size: 16px; font-weight: 700; }
            .head .a { font-size: 10.5px; }
            .head .t { margin-top: 5px; background: #000; color: #fff; font-weight: 700; font-size: 13px; padding: 4px; }
            .head .c { margin-top: 3px; font-size: 11px; }
            .k { background: #f0f0f0; font-weight: 700; white-space: nowrap; }
            .num { text-align: right; white-space: nowrap; }
            .ctr { text-align: center; }
            .sign { display: flex; justify-content: space-between; margin-top: 34px; font-size: 11px; }
            .sign div { border-top: 1px solid #000; width: 28%; text-align: center; padding-top: 3px; }";

        private static string Page(string title, string css, string body) =>
            $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{E(title)}</title><style>{BaseCss}{css}</style></head><body>{body}</body></html>";

        private static string Heading(string school, string address, string title, string? caption) =>
            $"<div class=\"head\"><div class=\"n\">{E(school)}</div><div class=\"a\">{E(address)}</div>" +
            $"<div class=\"t\">{E(title)}</div>{(string.IsNullOrWhiteSpace(caption) ? "" : $"<div class=\"c\">{E(caption)}</div>")}</div>";

        // ── Attendance card ────────────────────────────────────────────

        public static string AttendanceCard(AttendanceCardDto card, string school, string address)
        {
            var period = card.From.Date == card.To.Date ? Day(card.From) : $"{Day(card.From)} to {Day(card.To)}";
            var t = card.Totals;
            var sb = new StringBuilder();
            sb.Append(Heading(school, address, "Staff Attendance Card", period));
            sb.Append("<table style=\"margin-bottom:6px\">")
              .Append($"<tr><td class=\"k\">Name</td><td>{E(card.EmployeeName)}</td><td class=\"k\">Code</td><td>{E(card.EmployeeCode)}</td></tr>")
              .Append($"<tr><td class=\"k\">Designation</td><td>{E(card.DesignationName ?? "-")}</td><td class=\"k\">Campus</td><td>{E(card.CampusName ?? "-")}</td></tr>")
              .Append($"<tr><td class=\"k\">Period</td><td>{period}</td><td class=\"k\">Joining Date</td><td>{Day(card.JoiningDate)}</td></tr>")
              .Append("</table>");

            sb.Append("<table class=\"days\"><thead><tr>")
              .Append("<th style=\"width:5%\">Date</th><th style=\"width:5%\">Day</th><th style=\"width:20%\">Status</th>")
              .Append("<th style=\"width:12%\">Duty Time</th><th style=\"width:7%\">In</th><th style=\"width:7%\">Out</th>")
              .Append("<th style=\"width:7%\">Late (min)</th><th style=\"width:7%\">Early (min)</th><th style=\"width:7%\">Out of School (min)</th>")
              .Append("<th style=\"width:9%\">Deduction (Rs)</th><th>Remarks</th></tr></thead><tbody>");

            foreach (var d in card.Days)
            {
                var off = d.Status == 0 && (d.Label is "Holiday" or "Weekly Off" or "Not on staff");
                var cls = off ? "off" : d.Status == 2 ? "abs" : d.Label == "Not marked" ? "nm" : "";
                var label = d.Label == "Holiday" && !string.IsNullOrWhiteSpace(d.HolidayName) ? $"Holiday - {d.HolidayName}" : d.Label;
                var duty = d.DutyIn.HasValue && !off ? $"{Time(d.DutyIn)}-{Time(d.DutyOut)}" : "";
                var late = d.LateMinutes > 0 ? d.LateMinutes + (d.IgnoreLate ? "*" : "") : "";
                sb.Append($"<tr class=\"{cls}\"><td>{d.Date:dd}</td><td>{d.Date.ToString("ddd", Inv)}</td><td class=\"st\">{E(label)}</td>")
                  .Append($"<td>{duty}</td><td>{Time(d.TimeIn)}</td><td>{Time(d.TimeOut)}</td>")
                  .Append($"<td>{late}</td><td>{(d.EarlyMinutes > 0 ? d.EarlyMinutes : "")}</td><td>{(d.OutingMinutes > 0 ? d.OutingMinutes : "")}</td>")
                  .Append($"<td class=\"num\">{MoneyOrBlank(d.TotalDeduction)}</td><td style=\"text-align:left\">{E(d.Remarks)}</td></tr>");
            }
            sb.Append("</tbody></table>");

            if (card.Days.Any(d => d.IgnoreLate && d.LateMinutes > 0))
                sb.Append("<div style=\"font-size:9.5px;margin-top:2px\">* late arrival excused by the office - not deducted.</div>");
            if (t.NoSalaryRecord)
                sb.Append("<div style=\"font-size:10px;margin-top:2px;color:#b00000\"><b>No salary record</b> - deductions cannot be worked out.</div>");

            sb.Append("<table class=\"sum\"><tr><th>Present</th><th>Absent</th><th>Leave</th><th>Half Day</th><th>Short Leave</th><th>Exempt</th><th>Holidays</th><th>Weekly Off</th><th>Not Marked</th></tr>")
              .Append($"<tr><td>{t.PresentDays}</td><td>{t.AbsentDays}</td><td>{t.LeaveDays}</td><td>{t.HalfDays}</td><td>{t.ShortLeaveDays}</td><td>{t.ExemptDays}</td><td>{card.HolidayDays}</td><td>{card.OffDays}</td><td>{card.NotMarkedDays}</td></tr></table>");
            sb.Append("<table class=\"sum\"><tr><th></th><th>Days</th><th>Minutes</th><th>Deduction (Rs)</th></tr>")
              .Append($"<tr><th>Late Arrival</th><td>{t.LateCount}</td><td>{t.LateMinutes}</td><td>{Money(t.LateDeduction)}</td></tr>")
              .Append($"<tr><th>Early Leaving</th><td>{t.EarlyCount}</td><td>{t.EarlyMinutes}</td><td>{Money(t.EarlyDeduction)}</td></tr>")
              .Append($"<tr><th>Out of School</th><td>{t.OutingCount}</td><td>{t.OutingMinutes}</td><td>{Money(t.OutingDeduction)}</td></tr>")
              .Append($"<tr><th>Total</th><td></td><td>{t.LateMinutes + t.EarlyMinutes + t.OutingMinutes}</td><td><b>{Money(t.TotalDeduction)}</b></td></tr></table>");
            sb.Append("<div class=\"sign\"><div>Employee</div><div>Incharge / Clerk</div><div>Principal</div></div>");

            const string css = @"
                .days th, .days td { font-size: 10px; padding: 2px 4px; text-align: center; }
                .days tr.off td { background: #eeeeee; color: #444; }
                .days tr.abs td.st { color: #b00000; font-weight: 700; }
                .days tr.nm td.st { color: #b35c00; font-weight: 700; }
                .sum { margin-top: 6px; } .sum td, .sum th { font-size: 10.5px; text-align: center; }";
            return Page($"Attendance Card - {card.EmployeeName}", css, sb.ToString());
        }

        // ── Salary slip ────────────────────────────────────────────────

        public static string SalarySlip(PayslipDto p, string school, string address)
        {
            var month = p.PayMonth is >= 1 and <= 12
                ? new DateTime(p.PayYear, p.PayMonth, 1).ToString("MMMM yyyy", Inv) : "";

            string Line(string name, decimal amount, string? note = null) =>
                $"<tr><td>{E(name)}{(string.IsNullOrEmpty(note) ? "" : $" <span style=\"font-size:9.5px\">({E(note)})</span>")}</td><td class=\"num\">{Money(amount)}</td></tr>";

            var earn = new StringBuilder(Line("Basic Salary", p.Basic,
                p.WorkingDays > 0 && p.WorkingDays < p.TotalDays ? $"{Num(p.WorkingDays)} of {Num(p.TotalDays)} days" : null));
            foreach (var b in p.Breakdown.Where(b => b.Kind == 1 && b.Amount != 0)) earn.Append(Line(b.Name, b.Amount));
            // The incentives, every one listed so the slip shows what was and
            // was not paid. Mirrors salarySlipPrint.js on the web.
            earn.Append("<tr><td colspan=\"2\" class=\"sub\">Incentives</td></tr>");
            earn.Append(Line("Extra Daily", p.ExtraDaysPay, p.ExtraDays != 0 ? $"{Num(p.ExtraDays)} day(s) x {Money(p.DailyPay)}" : null));
            earn.Append(Line("Security Return", p.SecurityReturn));
            earn.Append(Line("Extra Duty", p.ExtraDuty));
            earn.Append(Line("Academy Duty", p.AcademyDuty));
            earn.Append(Line("Prize", p.Prize));
            if (p.Incentive != 0) earn.Append(Line("Incentive", p.Incentive));

            // The minutes behind each attendance charge are named on its line.
            int MinutesFor(string name) =>
                name.Contains("late", StringComparison.OrdinalIgnoreCase) ? p.LateMinutes
                : name.Contains("early", StringComparison.OrdinalIgnoreCase) ? p.EarlyMinutes
                : name.Contains("short leave", StringComparison.OrdinalIgnoreCase) || name.Contains("out of school", StringComparison.OrdinalIgnoreCase) ? p.OutingMinutes
                : 0;
            var ded = new StringBuilder();
            foreach (var b in p.Breakdown.Where(b => b.Kind == 2 && b.Amount != 0))
            {
                var m = MinutesFor(b.Name);
                ded.Append(Line(b.Name, b.Amount, b.Amount < 0 ? "bonus" : m > 0 ? $"{m} min" : null));
            }
            if (p.PayDeduction != 0) ded.Append(Line("Pay Deduction", p.PayDeduction));
            ded.Append(Line("Fine", p.Fine));
            if (p.SecurityDeduction != 0) ded.Append(Line("Security Deduction", p.SecurityDeduction));

            var take = Math.Round(p.TakeHome, 0, MidpointRounding.AwayFromZero);
            var sb = new StringBuilder();
            sb.Append(Heading(school, string.IsNullOrWhiteSpace(p.CampusName) ? address : $"{address}  |  {p.CampusName}", $"SALARY SLIP - {month}", null));
            // A slip from a payroll the office has not finalised (locked) yet.
            var proof = p.RunStatus == (byte)HrPayrollRunStatus.Draft;
            if (proof) sb.Append(ProofReadOnly);
            sb.Append("<table>")
              .Append($"<tr><td class=\"k\">Name</td><td>{E(p.EmployeeName)}</td><td class=\"k\">Code</td><td>{E(p.EmployeeCode)}</td><td class=\"k\">Designation</td><td>{E(p.DesignationName ?? "-")}</td></tr>")
              .Append($"<tr><td class=\"k\">Days in Month</td><td>{Num(p.TotalDays)}</td><td class=\"k\">Present</td><td>{Num(p.PresentDays)}</td><td class=\"k\">Absent / Leave</td><td>{Num(p.AbsentDays)} / {Num(p.LeaveDays)}</td></tr>")
              .Append($"<tr><td class=\"k\">Late (min)</td><td>{p.LateMinutes}</td><td class=\"k\">Early (min)</td><td>{p.EarlyMinutes}</td><td class=\"k\">Out of School (min)</td><td>{p.OutingMinutes}</td></tr>")
              .Append("</table>");
            sb.Append("<div style=\"display:flex;gap:8px;margin-top:6px;align-items:flex-start\">")
              .Append($"<table style=\"flex:1\"><tr><th colspan=\"2\">Earnings (Rs)</th></tr>{earn}<tr class=\"tot\"><td>Gross Salary</td><td class=\"num\">{Money(p.GrossEarning)}</td></tr></table>")
              .Append($"<table style=\"flex:1\"><tr><th colspan=\"2\">Deductions (Rs)</th></tr>{ded}<tr class=\"tot\"><td>Total Deduction</td><td class=\"num\">{Money(p.TotalDeduction)}</td></tr></table>")
              .Append("</div>");
            sb.Append("<table style=\"margin-top:6px\">")
              .Append($"<tr><td class=\"k\">Net Salary</td><td class=\"num\"><b>{Money(p.NetPay)}</b></td><td class=\"k\">Advance</td><td class=\"num\"><b>{Money(p.Advance)}</b></td>")
              .Append($"<td class=\"k\">Take Home</td><td class=\"num\" style=\"font-size:12.5px\"><b>Rs {take.ToString("N0", Inv)}</b></td></tr></table>");
            sb.Append($"<div style=\"font-size:10.5px;margin-top:4px\"><b>In words:</b> {Words((long)take)} Rupees Only.")
              .Append(p.PaidDate.HasValue ? $" &nbsp; <b>Paid:</b> {Day(p.PaidDate)}{(string.IsNullOrWhiteSpace(p.PaymentMethod) ? "" : $" ({E(p.PaymentMethod)})")}" : "")
              .Append("</div>");
            sb.Append("<div class=\"sign\"><div>Accountant</div><div>Principal</div><div>Received by</div></div>");

            const string css = "td, th { font-size: 10.5px; } tr.tot td { font-weight: 700; background: #f2f2f2; }" +
                               " td.sub { font-weight: 700; color: #11632f; background: #eaf6ee; font-size: 10px; }";
            return Page($"Salary Slip - {p.EmployeeName} - {month}", proof ? css + ProofCss : css, sb.ToString());
        }

        // Marks a salary slip whose payroll is still a draft: a red line under
        // the heading and "PROOF READ ONLY" across the page. Gone once the
        // payroll is locked. Mirrors salarySlipPrint.js on the web.
        private const string ProofReadOnly =
            "<div class=\"proof\">PROOF READ ONLY - not final. The salary may still change until the office finalises the payroll.</div>" +
            "<div class=\"proof-mark\">PROOF READ ONLY</div>";

        private const string ProofCss = @"
            .proof { border: 1.5px solid #c00; color: #c00; font-weight: 700; text-align: center; padding: 3px; margin-bottom: 6px; font-size: 11px; }
            .proof-mark { position: fixed; top: 12%; left: 0; right: 0; text-align: center; transform: rotate(-14deg);
                          font-size: 60px; font-weight: 700; color: rgba(204, 0, 0, .13); pointer-events: none; z-index: 10; }";

        /// <summary>Whole rupees in words: thousand, lakh, crore.</summary>
        public static string Words(long n)
        {
            if (n <= 0) return "Zero";
            string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
                              "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            string Two(long x) => x < 20 ? ones[x] : tens[x / 10] + (x % 10 > 0 ? " " + ones[x % 10] : "");
            string Three(long x) => (x >= 100 ? ones[x / 100] + " Hundred" + (x % 100 > 0 ? " " : "") : "") + (x % 100 > 0 ? Two(x % 100) : "");
            var parts = new List<string>();
            var crore = n / 10000000; n %= 10000000;
            var lakh = n / 100000; n %= 100000;
            var thousand = n / 1000; n %= 1000;
            if (crore > 0) parts.Add(Three(crore) + " Crore");
            if (lakh > 0) parts.Add(Two(lakh) + " Lakh");
            if (thousand > 0) parts.Add(Two(thousand) + " Thousand");
            if (n > 0) parts.Add(Three(n));
            return string.Join(" ", parts);
        }

        // ── Teacher's term result ──────────────────────────────────────

        /// <param name="rows">This teacher's subject results for the exam.</param>
        /// <param name="average">Mean of the subject percentages, as the Teacher Wise report ranks on.</param>
        /// <param name="position">e.g. "3rd of 25 teachers"; null when there is nothing to rank against.</param>
        public static string TeacherResult(string teacherName, string? designation, string examName,
            List<ExamTeacherWiseRowDto> rows, decimal average, string? position, string school, string address)
        {
            var sb = new StringBuilder();
            sb.Append(Heading(school, address, "Teacher Result", examName));
            sb.Append("<table style=\"margin-bottom:6px\">")
              .Append($"<tr><td class=\"k\">Teacher</td><td>{E(teacherName)}</td><td class=\"k\">Designation</td><td>{E(designation ?? "-")}</td></tr>")
              .Append($"<tr><td class=\"k\">Average Result</td><td><b>{Num(average)}%</b></td><td class=\"k\">Position</td><td><b>{E(position ?? "-")}</b></td></tr>")
              .Append("</table>");
            sb.Append("<table><thead><tr><th style=\"width:6%\">Sr.</th><th>Class - Section</th><th>Subject</th>")
              .Append("<th style=\"width:11%\">Appeared</th><th style=\"width:13%\">Total Marks</th><th style=\"width:13%\">Obtained</th><th style=\"width:11%\">Result %</th></tr></thead><tbody>");
            var i = 0;
            foreach (var r in rows.OrderBy(r => r.ClassName).ThenBy(r => r.SectionName).ThenBy(r => r.SubjectName))
            {
                var cls = string.Join(" - ", new[] { r.ClassName, r.SectionName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                sb.Append($"<tr><td class=\"ctr\">{++i}</td><td>{E(cls)}</td><td>{E(r.SubjectName)}</td><td class=\"ctr\">{r.Appeared}</td>")
                  .Append($"<td class=\"ctr\">{Num(r.TotalMarks ?? 0)}</td><td class=\"ctr\">{Num(r.ObtainedMarks ?? 0)}</td><td class=\"ctr\"><b>{Num(r.Percent ?? 0)}%</b></td></tr>");
            }
            sb.Append($"</tbody><tfoot><tr><td colspan=\"6\" class=\"num\" style=\"background:#f2f2f2\"><b>Average</b></td><td class=\"ctr\" style=\"background:#f2f2f2\"><b>{Num(average)}%</b></td></tr></tfoot></table>");
            // Same definition as the Teacher Wise Result report (SpRPT_SMSExamTeacherWise).
            sb.Append("<div style=\"font-size:10px;margin-top:4px\">Result % = (pass % of the students who appeared + obtained marks % of the total marks) / 2, ")
              .Append("as on the Teacher Wise Result report. The average is the mean of the subject results; the position ranks every teacher of the exam on it.</div>");
            sb.Append("<div class=\"sign\"><div>Teacher</div><div>Coordinator</div><div>Principal</div></div>");
            return Page($"Teacher Result - {teacherName}", "td, th { font-size: 11px; }", sb.ToString());
        }
    }
}
