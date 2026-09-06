// Services/Implementations/WhatsAppBotService.cs
using System.Text;
using School.API.DTOs.FeeManagement;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Implementations.Academic;
using School.API.Services.Implementations.Accounts;
using School.API.Services.Interfaces;
using School.API.Services.Interfaces.Academic;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations
{
    /// <summary>
    /// The self-service menu a parent gets when they message the school.
    /// </summary>
    /// <remarks>
    /// There is no stored conversation state, and that is deliberate. The reply
    /// is decided entirely from two facts available on every message: whether
    /// the sender's number is registered against a student, and whether the
    /// text is a bare menu digit. A menu that has to remember "what did I ask
    /// them last?" goes wrong as soon as two messages cross, a parent answers
    /// an hour later, or the app restarts — none of which can break this.
    ///
    /// Fee voucher and result card go out as the SAME PDFs the school already
    /// produces — the RDLC behind the printed A4 voucher, and the result card
    /// builder the exam campaign already sends. Nothing is re-drawn here, so a
    /// parent can hold the WhatsApp copy against the printed one and they match.
    ///
    /// A number is only ever shown the children it is registered against
    /// (student's own mobile, or father/mother/guardian), so one family cannot
    /// reach another's records.
    /// </remarks>
    public class WhatsAppBotService : IWhatsAppBotService
    {
        private readonly IWaBotRepository _repo;
        private readonly IFeeVoucherService _vouchers;
        private readonly ILedgerService _ledgers;
        private readonly ISMSExamResultService _examResults;
        private readonly IPdfRenderer _pdf;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppBotService> _logger;

        public WhatsAppBotService(
            IWaBotRepository repo,
            IFeeVoucherService vouchers,
            ILedgerService ledgers,
            ISMSExamResultService examResults,
            IPdfRenderer pdf,
            IConfiguration config,
            ILogger<WhatsAppBotService> logger)
        {
            _repo = repo;
            _vouchers = vouchers;
            _ledgers = ledgers;
            _examResults = examResults;
            _pdf = pdf;
            _config = config;
            _logger = logger;
        }

        private bool Enabled => _config.GetValue("WhatsAppBot:Enabled", true);
        private string SchoolName =>
            _config["WhatsAppBot:SchoolName"] ?? "Sir Syed Model High School";
        private string OfficePhone =>
            _config["WhatsAppBot:OfficePhone"] ?? "0333-6335634";

        public async Task<List<BotOutgoing>> ComposeReplyAsync(string waId, string? incomingText)
        {
            var outgoing = new List<BotOutgoing>();
            if (!Enabled) return outgoing;

            var text = (incomingText ?? string.Empty).Trim();
            var students = await _repo.GetStudentsByPhoneAsync(waId);

            // A bare 1–6 is a menu choice; anything else re-shows the menu.
            var isChoice = text.Length == 1 && text[0] >= '1' && text[0] <= '6';
            var choice = isChoice ? text[0] - '0' : 0;

            if (students.Count == 0)
            {
                outgoing.Add(BotOutgoing.Message(isChoice ? GeneralAnswer(choice) : GeneralMenu()));
                return outgoing;
            }

            if (!isChoice)
            {
                outgoing.Add(BotOutgoing.Message(StudentMenu(students)));
                return outgoing;
            }

            var all = choice == 6;

            // ── Text sections, gathered into one message ──────────────────
            if (choice is 1 or 5 || all)
            {
                var sb = new StringBuilder($"*{SchoolName}*").AppendLine().AppendLine();

                foreach (var s in students)
                {
                    if (students.Count > 1)
                        sb.AppendLine("────────────")
                          .AppendLine($"*{s.Name}*{Label(s)}")
                          .AppendLine();
                    else
                        sb.AppendLine($"*{s.Name}*{Label(s)}").AppendLine();

                    if (choice == 1 || all) sb.AppendLine(await DuesAsync(s)).AppendLine();
                    if (choice == 5 || all) sb.AppendLine(await AttendanceAsync(s)).AppendLine();
                }

                sb.AppendLine("_Reply with any other text to see the menu again._");
                outgoing.Add(BotOutgoing.Message(Fit(sb.ToString().TrimEnd())));
            }

            // ── PDFs: one document per child, because WhatsApp carries a
            //    single attachment per message ───────────────────────────
            if (choice == 2 || all)
                outgoing.AddRange(await VoucherDocumentsAsync(students, quiet: all));

            if (choice == 3 || all)
                outgoing.AddRange(await LedgerDocumentsAsync(students));

            // Result cards are genuinely per-child, so no de-duplication here.
            if (choice == 4 || all)
            {
                foreach (var s in students)
                {
                    var doc = await ResultPdfAsync(s);
                    if (doc is not null) outgoing.Add(doc);
                    else if (!all) outgoing.Add(BotOutgoing.Message(
                        $"*{s.Name}*{Label(s)}\nNo result published yet."));
                }
            }

            return outgoing;
        }

        private static string Label(BotStudentDto s) =>
            string.IsNullOrWhiteSpace(s.ClassLabel) ? "" : $" ({s.ClassLabel})";

        // ── PDF sections ─────────────────────────────────────────────────

        /// <summary>
        /// The family's most recent fee voucher(s), one PDF each.
        /// </summary>
        /// <remarks>
        /// A voucher belongs to the FAMILY, not the child — FeeVoucherMain is
        /// keyed by ParentID and a single receipt usually settles every
        /// sibling's fees at once. Asking each child for "their" last voucher
        /// therefore returns the same voucher again and again, and sending one
        /// PDF per child would deliver the identical document twice.
        ///
        /// So the vouchers are gathered first and de-duplicated by voucher id.
        /// Each distinct voucher is rendered once, and its caption names every
        /// child it covers — which is also more truthful than labelling a
        /// shared receipt with a single child's name.
        /// </remarks>
        private async Task<List<BotOutgoing>> VoucherDocumentsAsync(
            List<BotStudentDto> students, bool quiet)
        {
            var outgoing = new List<BotOutgoing>();

            var byVoucher = new Dictionary<decimal, (BotVoucherDto Voucher, List<BotStudentDto> Covers)>();
            var order = new List<decimal>();          // keeps the newest-first reading order
            var without = new List<BotStudentDto>();

            foreach (var s in students)
            {
                BotVoucherDto? v;
                try
                {
                    v = await _repo.GetLastVoucherAsync(s.StudentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WA-BOT] Last voucher lookup failed for student {Id}", s.StudentId);
                    continue;
                }

                if (v is null) { without.Add(s); continue; }

                if (!byVoucher.TryGetValue(v.Vid, out var entry))
                {
                    entry = (v, new List<BotStudentDto>());
                    byVoucher[v.Vid] = entry;
                    order.Add(v.Vid);
                }
                entry.Covers.Add(s);
            }

            foreach (var vid in order)
            {
                var (v, covers) = byVoucher[vid];
                try
                {
                    // Straight through the service the print button uses, so
                    // this is the identical A4 voucher — not a lookalike.
                    var pdf = await _vouchers.GetReceiptPdfAsync(v.Vid);
                    if (pdf is null) continue;

                    var who = covers.Count == 1
                        ? $"{covers[0].Name}{Label(covers[0])}"
                        : string.Join(", ", covers.Select(c => c.Name));

                    var caption =
                        $"*Fee Voucher — {who}*\n" +
                        $"Voucher No: {v.VNo}\n" +
                        $"Date: {v.VDate:dd MMM yyyy}\n" +
                        $"Amount Paid: Rs. {v.PaidAmount:N0}";

                    outgoing.Add(BotOutgoing.Document(caption, pdf.Value.Pdf, pdf.Value.FileName));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WA-BOT] Voucher PDF failed for voucher {Vid}", v.Vid);
                }
            }

            // On "All" the other sections already say plenty; only mention a
            // missing voucher when the parent asked for the voucher itself.
            if (!quiet && without.Count > 0)
            {
                var names = string.Join(", ", without.Select(s => s.Name));
                outgoing.Add(BotOutgoing.Message($"*{names}*\nNo fee voucher recorded yet."));
            }

            return outgoing;
        }

        /// <summary>
        /// The ledger as PDF — a single Family Ledger where the number covers
        /// more than one child, otherwise that child's Student Ledger.
        /// </summary>
        /// <remarks>
        /// Fee vouchers are taken against the family (FeeVoucherMain.ParentID)
        /// and one receipt routinely settles several children at once, so the
        /// family's account is the one that actually explains a balance. Sending
        /// a separate ledger per child would split one receipt across two
        /// statements, and neither would add up on its own.
        ///
        /// Children are grouped by ParentID rather than assumed to share one:
        /// a number can be registered against students whose records sit under
        /// different parent rows, and each of those is a separate account.
        /// </remarks>
        private async Task<List<BotOutgoing>> LedgerDocumentsAsync(List<BotStudentDto> students)
        {
            var outgoing = new List<BotOutgoing>();

            var families = students
                .Where(s => s.ParentId.HasValue)
                .GroupBy(s => s.ParentId!.Value)
                .ToList();

            var orphans = students.Where(s => !s.ParentId.HasValue).ToList();

            foreach (var family in families)
            {
                // One child under this parent reads better as their own ledger;
                // several belong on the family statement.
                if (family.Count() == 1)
                    orphans.Add(family.First());
                else
                    await AddAsync(
                        () => _ledgers.GetFamilyLedgerAsync(family.Key, null, null),
                        "Family Ledger",
                        $"Family_Ledger_{family.Key}.pdf",
                        string.Join(", ", family.Select(s => s.Name)));
            }

            foreach (var s in orphans)
                await AddAsync(
                    () => _ledgers.GetStudentLedgerAsync(s.StudentId, null, null),
                    "Student Ledger",
                    $"Ledger_{Safe(s.RollNo ?? s.StudentId.ToString())}.pdf",
                    $"{s.Name}{Label(s)}");

            return outgoing;

            async Task AddAsync(
                Func<Task<LedgerResponseDto>> fetch, string title, string fileName, string who)
            {
                try
                {
                    var data = await fetch();
                    if (data.Rows.Count == 0)
                    {
                        outgoing.Add(BotOutgoing.Message($"*{who}*\nNo ledger transactions recorded yet."));
                        return;
                    }

                    var html = LedgerHtmlBuilder.Build(
                        data, title, SchoolName, "Chhonawala Road, Hasilpur");
                    var pdf = await _pdf.RenderHtmlToPdfAsync(html);

                    var caption =
                        $"*{title} — {who}*\n" +
                        $"Total Debit: Rs. {data.TotalDebit:N0}\n" +
                        $"Total Credit: Rs. {data.TotalCredit:N0}\n" +
                        $"Balance: {Balance(data.ClosingBalance)}";

                    outgoing.Add(BotOutgoing.Document(caption, pdf, fileName));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WA-BOT] Ledger PDF failed for {Who}", who);
                }
            }
        }

        private static string Safe(string s) =>
            string.Concat(s.Where(char.IsLetterOrDigit));

        private async Task<BotOutgoing?> ResultPdfAsync(BotStudentDto s)
        {
            try
            {
                var rows = await _repo.GetLatestResultAsync(s.StudentId);
                if (rows.Count == 0) return null;

                var examId = rows[0].ExamId;

                // Same call and same builder the exam SMS campaign uses, so the
                // card is exactly what parents receive from the school.
                var card = await _examResults.GetResultCardAsync(
                    examId, s.ClassId, s.SectionId, 0, s.StudentId);

                var student = card?.Students?.FirstOrDefault();
                if (student is null) return null;

                var html = ResultCardHtmlBuilder.Build(student, rows[0].ExamName);
                var pdf = await _pdf.RenderHtmlToPdfAsync(html);

                var safeRoll = string.Concat((student.RollNo ?? s.StudentId.ToString())
                    .Where(char.IsLetterOrDigit));

                var total = rows.Sum(r => r.TotalMarks);
                var got = rows.Sum(r => r.ObtainedMarks);
                var pct = total == 0 ? 0 : Math.Round(got * 100m / total, 1);

                var caption =
                    $"*Result Card — {s.Name}*{Label(s)}\n" +
                    $"{rows[0].ExamName}\n" +
                    $"Total: {got:N0}/{total:N0} ({pct}%)";

                return BotOutgoing.Document(caption, pdf, $"ResultCard_{safeRoll}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Result card PDF failed for student {Id}", s.StudentId);
                return null;
            }
        }

        // ── Text sections ────────────────────────────────────────────────

        private async Task<string> DuesAsync(BotStudentDto s)
        {
            var dues = await _repo.GetPendingDuesAsync(s.StudentId);
            if (dues.Count == 0)
                return "*Pending Dues*\nNo dues outstanding. Thank you.";

            var sb = new StringBuilder("*Pending Dues*\n");
            foreach (var d in dues)
                sb.AppendLine($"• {d.FeeTypeName} {d.VDate:MMM-yyyy} — Rs. {d.PendingAmount:N0}");

            sb.Append($"*Total: Rs. {dues.Sum(d => d.PendingAmount):N0}*");
            return sb.ToString();
        }

        private async Task<string> AttendanceAsync(BotStudentDto s)
        {
            // The running session, not a rolling window — a parent asking in
            // September expects the year so far, not the last 30 days.
            var today = DateTime.Today;
            var from = new DateTime(today.Month >= 3 ? today.Year : today.Year - 1, 3, 1);

            var a = await _repo.GetAttendanceSummaryAsync(s.StudentId, from, today);
            if (a is null || a.Total == 0)
                return "*Attendance*\nNo attendance marked yet.";

            return new StringBuilder("*Attendance*\n")
                .AppendLine($"Since {from:dd MMM yyyy}")
                .AppendLine($"Present: {a.Present}")
                .AppendLine($"Absent: {a.Absent}")
                .AppendLine($"Leave: {a.Leave}")
                .Append($"*Attendance: {a.Percentage}%*")
                .ToString();
        }

        private static string Balance(decimal signed) =>
            Math.Abs(signed) < 0.005m
                ? "Rs. 0 (clear)"
                : signed > 0
                    ? $"Rs. {signed:N0} payable"
                    : $"Rs. {Math.Abs(signed):N0} advance";

        // WhatsApp rejects a body over 4096 characters, and Meta returns an
        // error rather than truncating — so a family with several children
        // asking for "All" would get nothing at all. Trim to a safe length and
        // say so, rather than losing the whole reply.
        private const int MaxBodyLength = 3900;

        private static string Fit(string body)
        {
            if (body.Length <= MaxBodyLength) return body;

            const string note = "\n\n_Message shortened. Reply with a single option (1–5) for the full detail._";
            var cut = body[..(MaxBodyLength - note.Length)];

            // Cut on a line boundary so a figure is never left half-printed.
            var lastBreak = cut.LastIndexOf('\n');
            if (lastBreak > 0) cut = cut[..lastBreak];

            return cut.TrimEnd() + note;
        }

        // ── Menus ─────────────────────────────────────────────────────────

        private string StudentMenu(List<BotStudentDto> students)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum").AppendLine();
            sb.AppendLine($"*{SchoolName}*").AppendLine();

            if (students.Count == 1)
            {
                sb.AppendLine($"{students[0].Name}{Label(students[0])}");
            }
            else
            {
                sb.AppendLine($"Registered students on this number ({students.Count}):");
                foreach (var s in students)
                    sb.AppendLine($"• {s.Name}{Label(s)}");
            }

            sb.AppendLine().AppendLine("Please reply with a number:").AppendLine();
            sb.AppendLine("*1* — Pending Dues");
            sb.AppendLine("*2* — Last Fee Voucher (PDF)");
            sb.AppendLine("*3* — Ledger (PDF)");
            sb.AppendLine("*4* — Result Card (PDF)");
            sb.AppendLine("*5* — Attendance");
            sb.AppendLine("*6* — All of the above");

            if (students.Count > 1)
                sb.AppendLine().AppendLine("_The reply covers all students listed above, each separately._");

            return sb.ToString().TrimEnd();
        }

        private string GeneralMenu()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Assalam o Alaikum").AppendLine();
            sb.AppendLine($"Welcome to *{SchoolName}*.").AppendLine();
            sb.AppendLine("Please reply with a number:").AppendLine();
            sb.AppendLine("*1* — New Admission");
            sb.AppendLine("*2* — Fee & Timings");
            sb.AppendLine("*3* — Contact & Location");
            sb.AppendLine("*4* — Talk to the office");
            sb.AppendLine();
            sb.AppendLine("_This number is not registered against any student._");
            sb.Append("_If you are a parent, please ask the office to register it._");
            return sb.ToString();
        }

        private string GeneralAnswer(int choice)
        {
            var body = choice switch
            {
                1 => "*New Admission*\n" +
                     "Admissions are open. Please visit the school office with:\n" +
                     "• Student's Form-B\n" +
                     "• Father's / Guardian's CNIC\n" +
                     "• Previous school result card (if any)\n" +
                     "• 2 passport-size photographs",

                2 => "*Fee & Timings*\n" +
                     "Fee depends on the class. Please contact the office for the current " +
                     "fee structure and school timings.",

                3 => "*Contact & Location*\n" +
                     $"{SchoolName}\n" +
                     "Chhonawala Road, Hasilpur\n" +
                     $"Phone: {OfficePhone}",

                _ => "*Talk to the office*\n" +
                     "Please send your question in this chat and a member of staff will reply " +
                     "during school hours, or call " + OfficePhone + "."
            };

            return $"{body}\n\n_Reply with any other text to see the menu again._";
        }
    }
}
