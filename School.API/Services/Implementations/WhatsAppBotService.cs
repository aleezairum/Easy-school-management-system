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
    /// The self-service menu a parent - or a member of staff - gets when they
    /// message the school.
    /// </summary>
    /// <remarks>
    /// There is no stored conversation state, and that is deliberate. The reply
    /// is decided entirely from facts available on every message: whether the
    /// sender's number is registered against a student, against a member of
    /// staff, both or neither, and whether the text is a bare menu number. A menu that has to remember "what did I ask
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
    public class WhatsAppBotService : IWhatsAppBotService, ISchoolDocuments
    {
        private readonly IWaBotRepository _repo;
        private readonly IFeeVoucherService _vouchers;
        private readonly ILedgerService _ledgers;
        private readonly ISMSExamResultService _examResults;
        private readonly IPdfRenderer _pdf;
        private readonly School.API.Services.Interfaces.HR.IHrReportsService _hrReports;
        private readonly School.API.Services.Interfaces.HR.IPayrollService _payroll;
        private readonly IConfiguration _config;
        private readonly ILogger<WhatsAppBotService> _logger;

        public WhatsAppBotService(
            IWaBotRepository repo,
            IFeeVoucherService vouchers,
            ILedgerService ledgers,
            ISMSExamResultService examResults,
            IPdfRenderer pdf,
            School.API.Services.Interfaces.HR.IHrReportsService hrReports,
            School.API.Services.Interfaces.HR.IPayrollService payroll,
            IConfiguration config,
            ILogger<WhatsAppBotService> logger)
        {
            _repo = repo;
            _vouchers = vouchers;
            _ledgers = ledgers;
            _examResults = examResults;
            _pdf = pdf;
            _hrReports = hrReports;
            _payroll = payroll;
            _config = config;
            _logger = logger;
        }

        private bool Enabled => _config.GetValue("WhatsAppBot:Enabled", true);

        // The school's COMPLETE name, not an abbreviation: it heads every reply,
        // and a parent who keeps or forwards one message has to be able to tell
        // who sent it. This is the same wording the printed reports carry
        // (FeeVoucherA4.rdlc, ResultCardHtmlBuilder, ledgerReport.js), so a
        // WhatsApp copy and a counter printout name the school identically.
        private string SchoolName =>
            _config["WhatsAppBot:SchoolName"] ?? "SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)";
        /// <summary>
        /// The number to quote when the campus is not known — an enquiry from a
        /// number the school has never seen, or a fallback if the campus list
        /// below is unconfigured.
        /// </summary>
        private string OfficePhone =>
            _config["WhatsAppBot:OfficePhone"]
            ?? CampusContacts.FirstOrDefault()?.Phone
            ?? "0301-7330101";
        private string Address =>
            _config["WhatsAppBot:Address"] ?? "Chhonawala Road, Hasilpur";

        /// <summary>
        /// The two-line identity block — name over address — that heads every
        /// reply, exactly as it heads the printed reports.
        /// </summary>
        private string Letterhead =>
            string.IsNullOrWhiteSpace(Address)
                ? $"*{SchoolName}*"
                : $"*{SchoolName}*\n{Address}";
        private string? WhatsAppNumber => _config["WhatsAppBot:WhatsAppNumber"];
        private string? Email => _config["WhatsAppBot:Email"];
        private string? Website => _config["WhatsAppBot:Website"];

        /// <summary>One campus and the number that rings on it.</summary>
        public sealed class CampusContact
        {
            /// <summary>SMSCampus.VID — what BotStudentDto.CampusId carries.</summary>
            public int Id { get; set; }

            /// <summary>Only so a human can check the id is the right one.</summary>
            public string? Name { get; set; }

            public string? Phone { get; set; }
        }

        /// <summary>
        /// The published number for each campus.
        /// </summary>
        /// <remarks>
        /// The school runs two campuses on two different lines, so "call the
        /// office" has to mean the office that actually holds the child's file.
        /// A registered parent gets their own campus's number; an enquiry from
        /// an unknown number gets both, because there is nothing to choose on.
        /// </remarks>
        private IReadOnlyList<CampusContact> CampusContacts =>
            _config.GetSection("WhatsAppBot:Campuses").Get<CampusContact[]>()
                ?.Where(c => !string.IsNullOrWhiteSpace(c.Phone)).ToList()
            ?? new List<CampusContact>();

        /// <summary>The campus's own number, or the office line if unknown.</summary>
        private string PhoneFor(int? campusId) =>
            CampusContacts.FirstOrDefault(c => c.Id == campusId)?.Phone ?? OfficePhone;

        /// <summary>
        /// One number when every child in the group is on the same campus;
        /// null when they are split, so the caller lists both instead of
        /// picking one of the two and being wrong for half the family.
        /// </summary>
        private string? PhoneForGroup(IEnumerable<BotStudentDto> students)
        {
            var campuses = students.Select(s => s.CampusId).Distinct().ToList();
            return campuses.Count == 1 ? PhoneFor(campuses[0]) : null;
        }

        /// <summary>Every campus and its number, one per line — for enquiries.</summary>
        private string ContactLines(string prefix = "📞 ")
        {
            var contacts = CampusContacts;
            if (contacts.Count == 0) return $"{prefix}{OfficePhone}";

            return string.Join("\n", contacts.Select(c =>
                string.IsNullOrWhiteSpace(c.Name)
                    ? $"{prefix}{c.Phone}"
                    : $"{prefix}{c.Name}: {c.Phone}"));
        }

        /// <summary>The same thing on one line, for a footer or a sentence.</summary>
        private string ContactInline()
        {
            var contacts = CampusContacts;
            if (contacts.Count == 0) return OfficePhone;

            return string.Join("  |  ", contacts.Select(c =>
                string.IsNullOrWhiteSpace(c.Name) ? c.Phone : $"{Short(c.Name!)} {c.Phone}"));

            // "Boys Campus" -> "Boys": the interactive-list footer is capped at
            // 60 characters by WhatsApp and Meta rejects the whole message
            // rather than trimming it.
            static string Short(string name) => name.Split(' ')[0];
        }

        /// <summary>
        /// The school's pin on Google Maps.
        /// </summary>
        /// <remarks>
        /// A share link from Maps when the school has set one; otherwise a Maps
        /// SEARCH for the school by name and address. The search link is the
        /// deliberate fallback rather than a hand-typed latitude and longitude —
        /// it always opens Maps, and a wrong co-ordinate would send a parent to
        /// a field outside town with no way to tell it was wrong.
        /// </remarks>
        private string MapUrl
        {
            get
            {
                var configured = _config["WhatsAppBot:MapUrl"];
                if (!string.IsNullOrWhiteSpace(configured)) return configured;

                // The parenthetical in the official name — "(BOYS & GIRLS)" —
                // is a description, not part of what Maps has indexed, and it
                // drags the search off the school. Everything else goes in.
                var name = System.Text.RegularExpressions.Regex
                    .Replace(SchoolName, @"\s*\([^)]*\)", string.Empty).Trim();

                var query = Uri.EscapeDataString($"{name} {Address}".Trim());
                return $"https://www.google.com/maps/search/?api=1&query={query}";
            }
        }

        private IReadOnlyList<string> About =>
            _config.GetSection("WhatsAppBot:About").Get<string[]>() ?? DefaultAbout;

        private IReadOnlyList<string> FeeNotes =>
            _config.GetSection("WhatsAppBot:FeeNotes").Get<string[]>() ?? DefaultFeeNotes;

        private static readonly string[] DefaultAbout =
        {
            "Separate Boys and Girls campuses, Montessori to Matric (10th)",
            "Merit-based monthly fee — a higher grade pays a lower fee",
            "Regular internal terms, result cards and parent-teacher meetings",
            "Qualified and experienced teaching staff",
            "Fee voucher, ledger, result and attendance available on WhatsApp"
        };

        private static readonly string[] DefaultFeeNotes =
        {
            "Fee of newly admitted students from class 4 to 10 is charged according to the grade attained in the admission test. It is increased or reduced in sequel of average performance in all next internal terms.",
            "No concession will be given in admission fee.",
            "This schedule is for the current academic session only and is revised every year."
        };

        private static readonly string[] DefaultAdmissionHighlights =
        {
            "From the next academic year a Computer class will also start for Pre-Ninth."
        };

        public async Task<List<BotOutgoing>> ComposeReplyAsync(string waId, string? incomingText)
        {
            if (!Enabled) return new List<BotOutgoing>();

            var students = await _repo.GetStudentsByPhoneAsync(waId);
            var staff = StaffEnabled ? await _repo.GetStaffByPhoneAsync(waId) : new List<BotStaffDto>();
            return await ComposeReplyForAsync(students, staff, incomingText);
        }

        /// <summary>
        /// The same menu and answers for a sender already identified - the
        /// mobile app's chat, where the signed-in account says who is asking
        /// instead of a phone number. Not switched off by WhatsAppBot:Enabled:
        /// the app chat costs nothing, so it keeps working while the paid
        /// WhatsApp auto-reply is off.
        /// </summary>
        public async Task<List<BotOutgoing>> ComposeReplyForAsync(List<BotStudentDto> students, List<BotStaffDto> staff, string? incomingText)
        {
            var outgoing = new List<BotOutgoing>();
            var text = (incomingText ?? string.Empty).Trim();

            // A number nobody is registered on: the enquiry menu, as always.
            if (students.Count == 0 && staff.Count == 0)
            {
                var g = MenuChoice(text, GeneralOptions.Length);
                if (g > 0) outgoing.AddRange(await GeneralAnswerAsync(g));
                else outgoing.Add(GeneralMenu());
                return Brand(outgoing);
            }

            // A bare number is a menu choice; anything else re-shows the menu.
            // The interactive list sends these same numbers as its row ids, so a
            // tapped option and a typed one take the identical path.
            //
            // Still no stored conversation state. A number that is BOTH a
            // parent's and a member of staff's gets both menus at once, and the
            // two never share a number: the parents' rows keep 1..8 (the digits
            // printed on the letter sent to parents) and the staff rows move up
            // to 11, 12... So "3" is always the ledger and "13" always this
            // month's salary slip - nothing has to remember which menu was last
            // shown. A number that is only staff gets the staff rows as 1, 2...
            var staffBase = students.Count > 0 ? StaffOffsetWhenBoth : 0;
            var choice = MenuChoice(text, 99);

            if (students.Count > 0 && choice >= 1 && choice <= StudentOptions.Length)
                outgoing.AddRange(await StudentAnswerAsync(students, choice));
            else if (staff.Count > 0 && choice > staffBase && choice <= staffBase + StaffOptions.Length)
                outgoing.AddRange(await StaffAnswerAsync(staff, choice - staffBase));
            else
            {
                if (students.Count > 0) outgoing.Add(StudentMenu(students));
                if (staff.Count > 0) outgoing.Add(StaffMenu(staff, staffBase, alsoParent: students.Count > 0));
            }

            return Brand(outgoing);
        }

        /// <summary>The number typed or tapped, 0 when the text is not one in range.</summary>
        private static int MenuChoice(string text, int max) =>
            text.Length is >= 1 and <= 2 && text.All(char.IsDigit) && int.TryParse(text, out var n) && n >= 1 && n <= max
                ? n : 0;

        private async Task<List<BotOutgoing>> StudentAnswerAsync(List<BotStudentDto> students, int choice)
        {
            if (choice == OptDiary) return await DiaryAsync(students);
            if (choice == OptAnnouncements)
                return await AnnouncementsAsync(audience: 1, students.Select(s => (int?)s.CampusId));

            var outgoing = new List<BotOutgoing>();
            var all = choice == OptAll;

            // ── Text sections, gathered into one message ──────────────────
            var perChild = choice is OptDues or OptTodayAttendance or OptAttendance || all;

            if (perChild || choice == OptTimings)
            {
                var sb = new StringBuilder(Letterhead).AppendLine().AppendLine();

                if (perChild)
                {
                    foreach (var s in students)
                    {
                        if (students.Count > 1)
                            sb.AppendLine("────────────")
                              .AppendLine($"*{s.Name}*{Label(s)}")
                              .AppendLine();
                        else
                            sb.AppendLine($"*{s.Name}*{Label(s)}").AppendLine();

                        if (choice == OptDues || all) sb.AppendLine(await DuesAsync(s)).AppendLine();
                        if (choice == OptTodayAttendance || all) sb.AppendLine(await TodayAttendanceAsync(s)).AppendLine();
                        if (choice == OptAttendance || all) sb.AppendLine(await AttendanceAsync(s)).AppendLine();
                    }
                }

                // Timings sit outside the per-child loop deliberately: children
                // sharing a timing are meant to be told once, and that grouping
                // cannot be decided one child at a time.
                if (choice == OptTimings || all)
                {
                    if (perChild && students.Count > 1) sb.AppendLine("────────────");
                    sb.AppendLine(await TimingsAsync(students)).AppendLine();
                }

                sb.AppendLine("_Reply with any other text to see the menu again._");
                outgoing.Add(BotOutgoing.Message(Fit(sb.ToString().TrimEnd())));
            }

            // ── PDFs: one document per child, because WhatsApp carries a
            //    single attachment per message ───────────────────────────
            if (choice == OptVoucher || all)
                outgoing.AddRange(await VoucherDocumentsAsync(students, quiet: all));

            if (choice == OptLedger || all)
                outgoing.AddRange(await LedgerDocumentsAsync(students));

            // Result cards are genuinely per-child, so no de-duplication here.
            if (choice == OptResult || all)
            {
                foreach (var s in students)
                {
                    var doc = await ResultPdfAsync(s);
                    if (doc is not null) outgoing.Add(doc);
                    else if (!all) outgoing.Add(BotOutgoing.Message(
                        $"*{s.Name}*{Label(s)}\nNo result published yet."));
                }
            }

            // "All of the above" covers the two rows above it as well.
            if (all)
            {
                outgoing.AddRange(await DiaryAsync(students, inAll: true));
                outgoing.AddRange(await AnnouncementsAsync(1, students.Select(s => (int?)s.CampusId), inAll: true));
            }

            return outgoing;
        }

        /// <summary>
        /// Puts the school's full name and address at the top of every reply
        /// that does not already carry it — PDF captions and one-line answers
        /// included.
        /// </summary>
        /// <remarks>
        /// Done in one place rather than at each call site so a section added
        /// later cannot go out unbranded. Messages that already name the school
        /// (both menus, the multi-child summary) are left alone: repeating it
        /// twice in the same bubble reads like a fault.
        /// </remarks>
        private List<BotOutgoing> Brand(List<BotOutgoing> outgoing)
        {
            foreach (var o in outgoing)
            {
                if (o.Text.Contains(SchoolName, StringComparison.OrdinalIgnoreCase)) continue;
                o.Text = Fit($"{Letterhead}\n\n{o.Text}");
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
                        $"Date: {v.VDate:dd-MMM-yyyy}\n" +
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

                    var html = LedgerHtmlBuilder.Build(data, title, SchoolName, Address);
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

        // ── For the mobile app (ISchoolDocuments) ───────────────────────

        public Task<List<BotOutgoing>> VoucherPdfsAsync(List<BotStudentDto> students) => VoucherDocumentsAsync(students, quiet: true);

        public Task<List<BotOutgoing>> LedgerPdfsAsync(List<BotStudentDto> students) => LedgerDocumentsAsync(students);

        public async Task<BotOutgoing?> ResultCardPdfAsync(BotStudentDto s, int examId, string examName)
        {
            try
            {
                // Same call and builder as the WhatsApp result card (see ResultPdfAsync).
                var card = await _examResults.GetResultCardAsync(examId, s.ClassId, sectionId: 0, prevExamId: 0, studentId: s.StudentId);
                var student = card?.Students?.FirstOrDefault();
                if (student is null) return null;
                var pdf = await _pdf.RenderHtmlToPdfAsync(ResultCardHtmlBuilder.Build(student, examName));
                var safeRoll = string.Concat((student.RollNo ?? s.StudentId.ToString()).Where(char.IsLetterOrDigit));
                return BotOutgoing.Document($"Result Card - {s.Name} - {examName}", pdf, $"ResultCard_{safeRoll}_{examId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[APP] Result card PDF failed for student {Id}, exam {Exam}", s.StudentId, examId);
                return null;
            }
        }

        public async Task<(BotOutgoing? Doc, string? Note)> AttendanceCardPdfAsync(BotStaffDto s, DateTime month)
        {
            var docs = new List<BotOutgoing>(); var notes = new List<string>();
            await AttendanceCardAsync(s, new DateTime(month.Year, month.Month, 1), docs, notes);
            return (docs.FirstOrDefault(), notes.FirstOrDefault());
        }

        public async Task<(BotOutgoing? Doc, string? Note)> SalarySlipPdfAsync(BotStaffDto s, DateTime month)
        {
            var docs = new List<BotOutgoing>(); var notes = new List<string>();
            await SalarySlipAsync(s, new DateTime(month.Year, month.Month, 1), docs, notes);
            return (docs.FirstOrDefault(), notes.FirstOrDefault());
        }

        private async Task<BotOutgoing?> ResultPdfAsync(BotStudentDto s)
        {
            try
            {
                var rows = await _repo.GetLatestResultAsync(s.StudentId);
                if (rows.Count == 0) return null;

                var examId = rows[0].ExamId;

                // Same call and same builder the exam SMS campaign uses, so the
                // card is exactly what parents receive from the school.
                //
                // sectionId 0, deliberately. The student id already picks out
                // the one card wanted, so a section adds nothing — and it can
                // take something away: a child whose exam rows were entered
                // under a section they have since left would match no rows at
                // all, and the parent would be told no result was published.
                // Position is class-wise inside the procedure either way.
                var card = await _examResults.GetResultCardAsync(
                    examId, s.ClassId, sectionId: 0, prevExamId: 0, studentId: s.StudentId);

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

        private async Task<string> TodayAttendanceAsync(BotStudentDto s)
        {
            var today = DateTime.Today;
            var a = await _repo.GetTodayAttendanceAsync(s.StudentId, today);

            var sb = new StringBuilder($"*Today's Attendance* ({today:dd-MMM-yyyy})\n");

            if (a is null)
            {
                // No register row. Saying "Absent" here would be a false alarm
                // to a parent — the class may simply not be marked yet, or it
                // may be a holiday.
                sb.Append("Not marked yet.");
                return sb.ToString();
            }

            sb.Append(a.Status);
            if (!string.IsNullOrWhiteSpace(a.Remarks))
                sb.Append($" — {a.Remarks}");

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
                return "*Attendance Summary*\nNo attendance marked yet.";

            return new StringBuilder("*Attendance Summary*\n")
                .AppendLine($"Since {from:dd-MMM-yyyy}")
                .AppendLine($"Present: {a.Present}")
                .AppendLine($"Absent: {a.Absent}")
                .AppendLine($"Leave: {a.Leave}")
                .Append($"*Attendance: {a.Percentage}%*")
                .ToString();
        }

        /// <summary>
        /// The children's own school timings, from the class-wise table.
        /// </summary>
        /// <remarks>
        /// Children on the SAME clock are listed together and the timing printed
        /// once. A parent with two children who both start at 7:30 and leave at
        /// 1:00 does not want to read the same four lines twice, and repeating
        /// them invites the reader to hunt for a difference that is not there.
        /// Children whose timings genuinely differ get a block each.
        ///
        /// Grouping is on the times alone, not on campus or class: two children
        /// in different classes, or on different campuses, that happen to run to
        /// the same clock are still one answer.
        ///
        /// A class the office has not filled in yet says so and gives the office
        /// number. Falling back to HrSchoolTiming would be worse than silence —
        /// that is the STAFF day, and teachers arrive before the children and
        /// leave after them.
        /// </remarks>
        private async Task<string> TimingsAsync(List<BotStudentDto> students)
        {
            var timings = await _repo.GetClassTimingsAsync();

            var matched = students
                .Select(s => (
                    Student: s,
                    Timing: timings.FirstOrDefault(
                        t => t.CampusId == s.CampusId && t.ClassId == s.ClassId)))
                .ToList();

            var sb = new StringBuilder("*School Timings*");

            foreach (var group in matched.Where(m => m.Timing is not null)
                                         .GroupBy(m => m.Timing!.Signature))
            {
                var t = group.First().Timing!;

                sb.AppendLine().AppendLine()
                  .AppendLine($"*{Who(group.Select(g => g.Student))}*")
                  .Append($"🕗 {Clock(t.TimeIn)} to {Clock(t.TimeOut)}");

                if (t.FridayTimeIn.HasValue && t.FridayTimeOut.HasValue)
                    sb.AppendLine()
                      .Append($"🕌 Friday: {Clock(t.FridayTimeIn.Value)} to {Clock(t.FridayTimeOut.Value)}");
            }

            var unset = matched.Where(m => m.Timing is null).Select(m => m.Student).ToList();
            if (unset.Count > 0)
            {
                // The number for THEIR campus — the office that holds the file.
                var phone = PhoneForGroup(unset);

                sb.AppendLine().AppendLine()
                  .AppendLine($"*{Who(unset)}*")
                  .Append("Timing not entered yet. Please call ")
                  .Append(phone is not null ? $"{phone}." : $"the office.\n{ContactLines()}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Names a group of children — with the class when there is only one,
        /// because then it is a heading rather than a list.
        /// </summary>
        private static string Who(IEnumerable<BotStudentDto> students)
        {
            var list = students.ToList();
            return list.Count == 1
                ? $"{list[0].Name}{Label(list[0])}"
                : string.Join(", ", list.Select(s => s.Name));
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

            const string note = "\n\n_Message shortened. Reply with a single option for the full detail._";
            var cut = body[..(MaxBodyLength - note.Length)];

            // Cut on a line boundary so a figure is never left half-printed.
            var lastBreak = cut.LastIndexOf('\n');
            if (lastBreak > 0) cut = cut[..lastBreak];

            return cut.TrimEnd() + note;
        }

        // ── Menus ─────────────────────────────────────────────────────────

        // ── Menus ─────────────────────────────────────────────────────────
        //
        // Sent as a WhatsApp interactive list: the parent taps a button and
        // picks a row rather than typing a number. The row ids are the plain
        // digits "1".."6", which is what makes the typed path still work — a
        // tapped row and a typed "3" arrive at the bot as the same string, so
        // there is one code path for both.
        //
        // Text is kept alongside as the fallback wording. If the interactive
        // send is refused for any reason, the reply falls back to the numbered
        // menu instead of failing outright.

        // The keycap emoji for a digit: the digit itself, then VARIATION
        // SELECTOR-16 and COMBINING ENCLOSING KEYCAP.
        //
        // Derived rather than listed. It used to be a fixed array of seven, and
        // adding an eighth menu row threw IndexOutOfRange on the very first
        // message — the menu is the one reply every parent sees.
        private static string Badge(int number) =>
            number is >= 1 and <= 9
                ? $"{(char)('0' + number)}️⃣"
                : $"{number}.";

        private static readonly (string Title, string Description)[] StudentOptions =
        {
            // Order matters: this is the order the rows appear in, and the
            // index+1 is the row id the parent sends back. "All of the above"
            // stays last (the school's choice, October 2026) - so adding the
            // diary and announcements moved it from 8 to 10. A letter to
            // parents that says "8 for everything" now needs reprinting.
            // WhatsApp shows at most 10 rows: this list is now full.
            ("Pending Dues",       "All unpaid fees to date"),
            ("Last Fee Voucher",   "Latest paid voucher as PDF"),
            ("Ledger",             "Full fee statement as PDF"),
            ("Result Card",        "Latest exam result as PDF"),
            ("Today's Attendance", "Was my child present today?"),
            ("Attendance Summary", "Present, absent and leave"),
            ("School Timings",     "My child's opening and closing time"),
            ("Today's Diary",      "Today's class diary as picture"),
            ("Announcements",      "Latest notices from the school"),
            ("All of the above",   "Everything in one go")
        };

        // The option numbers, named. The dispatch below reads as what it does
        // rather than as a list of magic digits, and OptAll cannot drift out of
        // step with the table above the way a hard-coded 7 did.
        private const int OptDues = 1;
        private const int OptVoucher = 2;
        private const int OptLedger = 3;
        private const int OptResult = 4;
        private const int OptTodayAttendance = 5;
        private const int OptAttendance = 6;
        private const int OptTimings = 7;
        private const int OptDiary = 8;
        private const int OptAnnouncements = 9;
        private static int OptAll => StudentOptions.Length;

        // The same treatment as the parents' menu: a tappable list whose row ids
        // are the plain digits, so tapping and typing take one code path.
        //
        // WhatsApp caps a row title at 24 characters and a description at 72,
        // and Meta REJECTS the whole message rather than trimming — so these
        // stay short deliberately. MetaWhatsAppSender clips as a last resort.
        private static readonly (string Title, string Description)[] GeneralOptions =
        {
            // No timings here on purpose. The school day is class-wise, and an
            // unregistered number does not tell us which class — quoting one
            // set of hours to everyone would be wrong for most of them. Parents
            // get their own child's timing from the registered menu instead.
            ("New Admission",       "Admission status and dates"),
            ("Fee Structure",       "Full dues detail as PDF"),
            ("Location",            "Google Maps link and address"),
            ("Contact & About Us",  "Phone, email and more"),
            ("Talk to the office",  "Write to our staff")
        };

        private BotOutgoing StudentMenu(List<BotStudentDto> students)
        {
            var who = new StringBuilder();
            if (students.Count == 1)
                who.Append(students[0].Name).Append(Label(students[0]));
            else
                who.Append(string.Join(", ", students.Select(s => s.Name)));

            var body = new StringBuilder();
            body.AppendLine("Assalam o Alaikum").AppendLine();
            body.AppendLine("Welcome to").AppendLine(Letterhead).AppendLine();

            if (students.Count == 1)
            {
                body.AppendLine($"*{students[0].Name}*{Label(students[0])}");
            }
            else
            {
                body.AppendLine($"Registered students on this number ({students.Count}):");
                foreach (var s in students)
                    body.AppendLine($"• *{s.Name}*{Label(s)}");
            }

            body.AppendLine().AppendLine("Please choose what you would like to see:");

            // Fallback wording — also what a parent sees if their WhatsApp is
            // too old to render an interactive list.
            var text = new StringBuilder(body.ToString()).AppendLine();
            for (var i = 0; i < StudentOptions.Length; i++)
                text.AppendLine($"{Badge(i + 1)} *{StudentOptions[i].Title}*");
            text.AppendLine().Append("_Reply with the number, e.g. 1_");

            var list = new SmsInteractiveList
            {
                Body = body.ToString().TrimEnd(),
                Footer = students.Count > 1 ? "Covers all students listed" : SchoolName,
                ButtonText = "View Options",
                Rows = StudentOptions.Select((o, i) => new SmsInteractiveRow
                {
                    Id = (i + 1).ToString(),
                    Title = o.Title,
                    Description = o.Description
                }).ToList()
            };

            return BotOutgoing.Menu(text.ToString(), list);
        }

        private BotOutgoing GeneralMenu()
        {
            var body = new StringBuilder();
            body.AppendLine("Assalam o Alaikum").AppendLine();
            body.AppendLine("Welcome to").AppendLine(Letterhead).AppendLine();
            body.AppendLine("This number is not registered against any student.");
            body.AppendLine("If you are a parent, please ask the office to register it.")
                .AppendLine();
            body.Append("How can we help you?");

            var text = new StringBuilder(body.ToString()).AppendLine().AppendLine();
            for (var i = 0; i < GeneralOptions.Length; i++)
                text.AppendLine($"{Badge(i + 1)} *{GeneralOptions[i].Title}*");
            text.AppendLine().Append("_Reply with the number, e.g. 1_");

            var list = new SmsInteractiveList
            {
                Body = body.ToString(),
                Footer = ContactInline(),
                ButtonText = "View Options",
                Rows = GeneralOptions.Select((o, i) => new SmsInteractiveRow
                {
                    Id = (i + 1).ToString(),
                    Title = o.Title,
                    Description = o.Description
                }).ToList()
            };

            return BotOutgoing.Menu(text.ToString(), list);
        }

        // ── Daily diary and announcements ────────────────────────────────

        /// <summary>
        /// Today's diary pages for each child's class and section, as pictures.
        /// Children in the same class and section share a diary, and it is sent
        /// once with both names on it.
        /// </summary>
        /// <param name="inAll">Part of "All of the above": no closing menu hint.</param>
        private async Task<List<BotOutgoing>> DiaryAsync(List<BotStudentDto> students, bool inAll = false)
        {
            var outgoing = new List<BotOutgoing>();
            var today = DateTime.Today;
            var missing = new List<BotStudentDto>();
            var sent = new Dictionary<int, List<BotStudentDto>>();      // diary id -> children it covers
            var pagesById = new Dictionary<int, List<BotDiaryPageDto>>();

            foreach (var s in students)
            {
                var pages = s.ClassId > 0 ? await _repo.GetDiaryPagesAsync(s.ClassId, s.SectionId, today) : new();
                if (pages.Count == 0) { missing.Add(s); continue; }
                foreach (var g in pages.GroupBy(p => p.DiaryId))
                {
                    if (!sent.TryGetValue(g.Key, out var covers)) { sent[g.Key] = covers = new(); pagesById[g.Key] = g.ToList(); }
                    covers.Add(s);
                }
            }

            foreach (var (diaryId, covers) in sent)
            {
                var pages = pagesById[diaryId];
                for (var i = 0; i < pages.Count; i++)
                {
                    var p = pages[i];
                    var caption = new StringBuilder($"*Today's Diary — {Who(covers)}*\n{today:dddd, dd-MMM-yyyy}");
                    if (pages.Count > 1) caption.Append($"\nPage {i + 1} of {pages.Count}");
                    if (i == 0 && !string.IsNullOrWhiteSpace(p.Notes)) caption.Append($"\n\n{p.Notes}");
                    outgoing.Add(BotOutgoing.Image(caption.ToString(), p.Data, p.FileName));
                }
            }

            if (missing.Count > 0)
                outgoing.Add(BotOutgoing.Message(
                    $"*Today's Diary* ({today:dd-MMM-yyyy})\n\n" +
                    string.Join("\n", missing.Select(s => $"• *{s.Name}*{Label(s)} — not uploaded yet.")) +
                    "\n\nPlease check again later in the day."));

            if (!inAll) outgoing[^1].Text += "\n\n_Reply with any other text to see the menu again._";
            return outgoing;
        }

        /// <summary>
        /// The school's announcements running today, for parents (1) or staff
        /// (2), on the campuses given. One with a picture goes as that picture;
        /// the rest are gathered into one message.
        /// </summary>
        /// <param name="inAll">Part of "All of the above": nothing at all when there are none, and no closing menu hint.</param>
        private async Task<List<BotOutgoing>> AnnouncementsAsync(byte audience, IEnumerable<int?> campusIds, bool inAll = false)
        {
            var outgoing = new List<BotOutgoing>();
            var items = await _repo.GetCurrentAnnouncementsAsync(audience, campusIds);

            if (items.Count == 0 && inAll) return outgoing;
            if (items.Count == 0)
            {
                outgoing.Add(BotOutgoing.Message("*Announcements*\n\nThere are no announcements at the moment." +
                                                 "\n\n_Reply with any other text to see the menu again._"));
                return outgoing;
            }

            static string Body(BotAnnouncementDto a) => $"📢 *{a.Title}*\n{a.Message}";

            // A picture's caption is capped at 1024 characters by WhatsApp, and
            // the school's name goes on top of it: a long message follows the
            // picture as its own text instead.
            foreach (var a in items.Where(a => a.ImageData is { Length: > 0 }))
            {
                var body = Body(a);
                if (body.Length <= 850)
                    outgoing.Add(BotOutgoing.Image(body, a.ImageData!, a.ImageFileName ?? "announcement.jpg"));
                else
                {
                    outgoing.Add(BotOutgoing.Image($"📢 *{a.Title}*", a.ImageData!, a.ImageFileName ?? "announcement.jpg"));
                    outgoing.Add(BotOutgoing.Message(body));
                }
            }

            var plain = items.Where(a => a.ImageData is not { Length: > 0 }).ToList();
            if (plain.Count > 0)
                outgoing.Add(BotOutgoing.Message(Fit("*Announcements*\n\n" + string.Join("\n\n────────────\n\n", plain.Select(Body)))));

            if (!inAll) outgoing[^1].Text += "\n\n_Reply with any other text to see the menu again._";
            return outgoing;
        }

        // ── Staff ─────────────────────────────────────────────────────────
        //
        // A member of staff messaging from their own number (Employees.Mobile
        // or Phone) gets their own attendance card and salary slip as PDF, and
        // a teacher their latest term result. Only ever their own: everything
        // is looked up by the employee id the number resolved to.

        /// <summary>Set WhatsAppBot:Staff:Enabled to false to answer staff as before (parents / enquiry only).</summary>
        private bool StaffEnabled => _config.GetValue("WhatsAppBot:Staff:Enabled", true);

        /// <summary>
        /// Send salary slips from a payroll run still in Draft, so staff can
        /// check them before the office finalises. On by default: a draft can
        /// still change, so its slip is marked "PROOF READ ONLY" (the PDF and
        /// the caption). Once the run is locked the mark is gone.
        /// </summary>
        private bool DraftSlips => _config.GetValue("WhatsAppBot:Staff:SendDraftSalarySlips", true);

        /// <summary>Where the staff rows start when the number also has the parents' menu.</summary>
        private const int StaffOffsetWhenBoth = 10;

        private static readonly (string Title, string Description)[] StaffOptions =
        {
            // Same rule as the parents' menu: "All of the above" stays last.
            ("Attendance - This Month", "Attendance card as PDF"),
            ("Attendance - Last Month", "Attendance card as PDF"),
            ("Salary Slip - This Month", "Salary slip as PDF"),
            ("Salary Slip - Last Month", "Salary slip as PDF"),
            ("Term Result (Teachers)", "Your classes' latest result as PDF"),
            ("Announcements", "Latest notices from the school"),
            ("All of the above", "Everything in one go")
        };

        private const int StaffAttThis = 1;
        private const int StaffAttLast = 2;
        private const int StaffSlipThis = 3;
        private const int StaffSlipLast = 4;
        private const int StaffResult = 5;
        private const int StaffAnnouncements = 6;
        private static int StaffAll => StaffOptions.Length;

        private BotOutgoing StaffMenu(List<BotStaffDto> staff, int offset, bool alsoParent)
        {
            var body = new StringBuilder();
            if (alsoParent)
            {
                // The parents' menu above already greeted them.
                body.AppendLine("*Staff Services*").AppendLine();
                body.AppendLine("This number is also registered as staff:");
            }
            else
            {
                body.AppendLine("Assalam o Alaikum").AppendLine();
                body.AppendLine("Welcome to").AppendLine(Letterhead).AppendLine();
                body.AppendLine("*Staff Services*");
            }
            foreach (var s in staff)
                body.AppendLine($"• *{s.Name}*{StaffLabel(s)}");
            body.AppendLine().AppendLine("Please choose what you would like to receive:");

            var text = new StringBuilder(body.ToString()).AppendLine();
            for (var i = 0; i < StaffOptions.Length; i++)
                text.AppendLine($"{Badge(offset + i + 1)} *{StaffOptions[i].Title}*");
            text.AppendLine().Append($"_Reply with the number, e.g. {offset + 1}_");

            var list = new SmsInteractiveList
            {
                Body = body.ToString().TrimEnd(),
                Footer = SchoolName,
                ButtonText = alsoParent ? "Staff Options" : "View Options",
                Rows = StaffOptions.Select((o, i) => new SmsInteractiveRow
                {
                    Id = (offset + i + 1).ToString(),
                    Title = o.Title,
                    Description = o.Description
                }).ToList()
            };

            return BotOutgoing.Menu(text.ToString(), list);
        }

        private static string StaffLabel(BotStaffDto s)
        {
            var parts = new[] { s.DesignationName, s.CampusName }.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return parts.Count == 0 ? "" : $" ({string.Join(", ", parts)})";
        }

        private async Task<List<BotOutgoing>> StaffAnswerAsync(List<BotStaffDto> staff, int choice)
        {
            if (choice == StaffAnnouncements)
                return await AnnouncementsAsync(audience: 2, staff.Select(s => s.CampusId));

            var outgoing = new List<BotOutgoing>();
            var notes = new List<string>();     // what could not be sent, said once at the end
            var all = choice == StaffAll;
            var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            foreach (var s in staff)
            {
                if (choice == StaffAttThis || all) await AttendanceCardAsync(s, thisMonth, outgoing, notes);
                if (choice == StaffAttLast || all) await AttendanceCardAsync(s, lastMonth, outgoing, notes);
                if (choice == StaffSlipThis || all) await SalarySlipAsync(s, thisMonth, outgoing, notes);
                if (choice == StaffSlipLast || all) await SalarySlipAsync(s, lastMonth, outgoing, notes);
                // "All" from someone who teaches nothing does not need telling so.
                if (choice == StaffResult || all) await TeacherResultAsync(s, outgoing, notes, quiet: all);
            }

            // "All of the above" covers the Announcements row above it as well.
            if (all) outgoing.AddRange(await AnnouncementsAsync(2, staff.Select(s => s.CampusId), inAll: true));

            if (notes.Count > 0 || outgoing.Count == 0)
                outgoing.Add(BotOutgoing.Message(string.Join("\n\n", notes.DefaultIfEmpty("Nothing to send for this option yet."))
                    + "\n\n_Reply with any other text to see the menu again._"));
            else
                outgoing[^1].Text += "\n\n_Reply with any other text to see the menu again._";

            return outgoing;
        }

        private async Task AttendanceCardAsync(BotStaffDto s, DateTime month, List<BotOutgoing> outgoing, List<string> notes)
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            try
            {
                var cards = await _hrReports.GetAttendanceCardsAsync(month, monthEnd,
                    new School.API.DTOs.HR.HrReportFilter { EmployeeId = s.EmployeeId });
                var card = cards.FirstOrDefault();
                if (card is null)
                {
                    notes.Add($"*{s.Name}* — no attendance card for {month:MMMM yyyy} (not on the staff that month).");
                    return;
                }

                var html = HR.StaffDocumentsHtmlBuilder.AttendanceCard(card, SchoolName, Address);
                var pdf = await _pdf.RenderHtmlToPdfAsync(html);
                var t = card.Totals;
                var caption =
                    $"*Attendance Card — {s.Name}*\n" +
                    $"{month:MMMM yyyy}{(month.Month == DateTime.Today.Month && month.Year == DateTime.Today.Year ? $" (up to {DateTime.Today:dd-MMM})" : "")}\n" +
                    $"Present: {t.PresentDays}  |  Absent: {t.AbsentDays}  |  Leave: {t.LeaveDays}\n" +
                    $"Late: {t.LateMinutes} min  |  Early: {t.EarlyMinutes} min  |  Out: {t.OutingMinutes} min\n" +
                    (t.NoSalaryRecord ? "Deduction: not worked out (no salary record)" : $"Deduction: Rs. {t.TotalDeduction:N2}") +
                    (card.NotMarkedDays > 0 ? $"\nNot marked: {card.NotMarkedDays} day(s)" : "");
                outgoing.Add(BotOutgoing.Document(caption, pdf, $"Attendance_{Safe(s.EmployeeCode)}_{month:yyyy_MM}.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Attendance card failed for employee {Id}", s.EmployeeId);
                notes.Add($"*{s.Name}* — the attendance card for {month:MMMM yyyy} could not be prepared right now.");
            }
        }

        private async Task SalarySlipAsync(BotStaffDto s, DateTime month, List<BotOutgoing> outgoing, List<string> notes)
        {
            try
            {
                var slips = await _payroll.GetPayslipsForPeriodAsync(month, month.AddMonths(1).AddDays(-1), null, null, s.EmployeeId);
                if (slips.Count == 0)
                {
                    notes.Add($"*{s.Name}* — the salary for {month:MMMM yyyy} has not been prepared yet.");
                    return;
                }

                foreach (var p in slips)
                {
                    var draft = p.RunStatus == (byte)School.API.Data.DBModels.HR.HrPayrollRunStatus.Draft;
                    if (draft && !DraftSlips)
                    {
                        notes.Add($"*{s.Name}* — the salary for {month:MMMM yyyy} is still being prepared. " +
                                  "The slip will be available here once the office finalises it.");
                        continue;
                    }

                    var html = HR.StaffDocumentsHtmlBuilder.SalarySlip(p, SchoolName, Address);
                    var pdf = await _pdf.RenderHtmlToPdfAsync(html);
                    var caption =
                        (draft ? "*PROOF READ ONLY* — not final; the salary may still change until the office finalises it.\n\n" : "") +
                        $"*Salary Slip — {s.Name}*\n" +
                        $"{month:MMMM yyyy}\n" +
                        $"Gross: Rs. {p.GrossEarning:N0}  |  Deduction: Rs. {p.TotalDeduction:N0}\n" +
                        $"*Take Home: Rs. {Math.Round(p.TakeHome, 0, MidpointRounding.AwayFromZero):N0}*" +
                        (p.PaidDate.HasValue ? $"\nPaid on {p.PaidDate:dd-MMM-yyyy}" : "");
                    outgoing.Add(BotOutgoing.Document(caption, pdf, $"SalarySlip_{Safe(s.EmployeeCode)}_{month:yyyy_MM}.pdf"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Salary slip failed for employee {Id}", s.EmployeeId);
                notes.Add($"*{s.Name}* — the salary slip for {month:MMMM yyyy} could not be prepared right now.");
            }
        }

        /// <summary>
        /// A teacher's latest term result: every subject assigned to them on
        /// the Staff Subjects screen for the newest exam with marks, ranked
        /// against every teacher of that exam exactly as the Teacher Wise
        /// Result report ranks them (mean of the subject results).
        /// </summary>
        private async Task TeacherResultAsync(BotStaffDto s, List<BotOutgoing> outgoing, List<string> notes, bool quiet)
        {
            try
            {
                var exam = await _repo.GetTeacherLatestExamAsync(s.EmployeeId);
                if (exam is null)
                {
                    if (!quiet) notes.Add($"*{s.Name}* — no term result found for subjects assigned to you.");
                    return;
                }

                var everyone = await _examResults.GetTeacherWiseReportAsync(exam.Value.ExamId, 0);
                var mine = everyone.Where(r => r.TeacherId == s.EmployeeId).ToList();
                if (mine.Count == 0)
                {
                    if (!quiet) notes.Add($"*{s.Name}* — no term result found for subjects assigned to you.");
                    return;
                }

                // Rank as the report does: average of the subject percentages,
                // highest first; equal averages share a position.
                var averages = everyone.Where(r => r.TeacherId.HasValue)
                    .GroupBy(r => r.TeacherId!.Value)
                    .Select(g => (Id: g.Key, Avg: Math.Round(g.Average(r => r.Percent ?? 0), 2)))
                    .ToList();
                var myAvg = averages.First(a => a.Id == s.EmployeeId).Avg;
                var rank = averages.Count(a => a.Avg > myAvg) + 1;
                var position = $"{Ordinal(rank)} of {averages.Count} teachers";

                var html = HR.StaffDocumentsHtmlBuilder.TeacherResult(s.Name, s.DesignationName, exam.Value.ExamName,
                                                                      mine, myAvg, position, SchoolName, Address);
                var pdf = await _pdf.RenderHtmlToPdfAsync(html);
                var caption =
                    $"*Term Result — {s.Name}*\n" +
                    $"{exam.Value.ExamName}\n" +
                    $"{mine.Count} subject(s)  |  Average: {myAvg:0.##}%\n" +
                    $"Position: {position}";
                outgoing.Add(BotOutgoing.Document(caption, pdf, $"TeacherResult_{Safe(s.EmployeeCode)}.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Teacher result failed for employee {Id}", s.EmployeeId);
                if (!quiet) notes.Add($"*{s.Name}* — the term result could not be prepared right now.");
            }
        }

        private static string Ordinal(int n) =>
            n % 100 is >= 11 and <= 13 ? n + "th"
            : n + (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

        // ── General answers (unregistered numbers) ───────────────────────
        //
        // These are what a prospective parent gets, and they are answered from
        // the system wherever the system knows: the fee schedule out of
        // SMSFeeStructure, the timings out of the HR timing calendar. Only the
        // things the database has no place for — admission dates, the map pin,
        // the publicity lines — come from configuration, so the office can
        // change them without a rebuild.

        private async Task<List<BotOutgoing>> GeneralAnswerAsync(int choice)
        {
            var outgoing = choice switch
            {
                1 => new List<BotOutgoing> { BotOutgoing.Message(AdmissionAnswer()) },
                2 => await FeeStructureAnswerAsync(),
                3 => new List<BotOutgoing> { BotOutgoing.Message(LocationAnswer()) },
                4 => new List<BotOutgoing> { BotOutgoing.Message(ContactAnswer()) },
                _ => new List<BotOutgoing> { BotOutgoing.Message(OfficeAnswer()) }
            };

            if (outgoing.Count > 0)
                outgoing[^1].Text += "\n\n_Reply with any other text to see the menu again._";

            return outgoing;
        }

        private string AdmissionAnswer()
        {
            var open = _config.GetValue("WhatsAppBot:Admission:Open", false);
            var opensOn = _config["WhatsAppBot:Admission:OpensOn"] ?? "January 2027";
            var highlights = _config.GetSection("WhatsAppBot:Admission:Highlights").Get<string[]>()
                             ?? DefaultAdmissionHighlights;

            var sb = new StringBuilder("*New Admission*").AppendLine().AppendLine();

            if (open)
            {
                sb.AppendLine("Admissions are open. Please visit the school office with:")
                  .AppendLine("• Student's Form-B")
                  .AppendLine("• Father's / Guardian's CNIC")
                  .AppendLine("• Previous school result card (if any)")
                  .AppendLine("• 2 passport-size photographs")
                  .AppendLine();
            }
            else
            {
                sb.AppendLine("Admissions for the running academic session are now *closed*.")
                  .AppendLine();

                if (!string.IsNullOrWhiteSpace(opensOn))
                    sb.AppendLine($"📅 Admissions for the next session will start from *{opensOn}*.")
                      .AppendLine();
            }

            foreach (var h in highlights.Where(h => !string.IsNullOrWhiteSpace(h)))
                sb.AppendLine($"✨ {h}").AppendLine();

            return sb.AppendLine("Reply 2 for the fee structure, or call:")
                     .Append(ContactLines())
                     .ToString();
        }

        /// <summary>
        /// The dues-detail sheet as a PDF, built from the fee structure the
        /// school actually charges on.
        /// </summary>
        /// <remarks>
        /// A PDF rather than text because the schedule is a grade-by-class
        /// matrix — seven grade bands across seven class groups — which no
        /// amount of formatting survives in a WhatsApp bubble. It is generated
        /// per request rather than stored: the fee is re-entered every session,
        /// and a cached file would go on quoting last year's figures.
        /// </remarks>
        private async Task<List<BotOutgoing>> FeeStructureAnswerAsync()
        {
            var data = await _repo.GetFeeStructureAsync();

            if (data is null || !data.HasData)
                return new List<BotOutgoing>
                {
                    BotOutgoing.Message(
                        "*Fee Structure*\n\n" +
                        "The fee schedule for the current session is not available on this " +
                        "number yet. Please call the office and they will share it.\n\n" +
                        ContactLines())
                };

            try
            {
                var html = FeeStructureHtmlBuilder.Build(data, SchoolName, Address, FeeNotes);
                var pdf = await _pdf.RenderHtmlToPdfAsync(html);

                // Sessions are named "Year 2026-27" in the system; the word is
                // dropped so the sentence does not read "the academic Year".
                var session = data.SessionName.Replace("Year ", "", StringComparison.OrdinalIgnoreCase).Trim();

                var sb = new StringBuilder("*Fee Structure*").AppendLine().AppendLine()
                    .AppendLine($"Dues detail for the academic year {session}.")
                    .AppendLine();

                var campuses = data.Campuses.Where(c => c.Schedules.Count > 0)
                                            .Select(c => c.Name).ToList();
                if (campuses.Count > 0)
                    sb.AppendLine($"🏫 {string.Join("  |  ", campuses)}");

                sb.AppendLine()
                  .AppendLine("The monthly fee is charged on the grade obtained in the internal " +
                              "terms / admission test — a higher grade pays a lower fee.")
                  .AppendLine()
                  .Append("_Fee is revised every academic year._");

                var file = $"Fee_Structure_{Safe(data.SessionName)}.pdf";
                return new List<BotOutgoing> { BotOutgoing.Document(sb.ToString(), pdf, file) };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WA-BOT] Fee structure PDF failed");
                return new List<BotOutgoing>
                {
                    BotOutgoing.Message(
                        "*Fee Structure*\n\n" +
                        "Sorry, the fee sheet could not be prepared right now. Please call:\n" +
                        ContactLines())
                };
            }
        }

        private string LocationAnswer()
        {
            var sb = new StringBuilder("*Location*").AppendLine().AppendLine()
                .AppendLine($"*{SchoolName}*");

            if (!string.IsNullOrWhiteSpace(Address))
                sb.AppendLine($"📍 {Address}");

            return sb.AppendLine()
                     .AppendLine("🗺️ Open in Google Maps:")
                     .AppendLine(MapUrl)
                     .AppendLine()
                     .Append("Tap the link for directions to the school.")
                     .ToString();
        }

        private string ContactAnswer()
        {
            var sb = new StringBuilder("*Contact & About Us*").AppendLine().AppendLine()
                .AppendLine($"*{SchoolName}*");

            if (!string.IsNullOrWhiteSpace(Address))
                sb.AppendLine($"📍 {Address}");

            sb.AppendLine(ContactLines());

            if (!string.IsNullOrWhiteSpace(WhatsAppNumber))
                sb.AppendLine($"💬 WhatsApp: {WhatsAppNumber}");
            if (!string.IsNullOrWhiteSpace(Email))
                sb.AppendLine($"✉️ {Email}");
            if (!string.IsNullOrWhiteSpace(Website))
                sb.AppendLine($"🌐 {Website}");

            sb.AppendLine($"🗺️ {MapUrl}");

            var about = About;
            if (about.Count > 0)
            {
                sb.AppendLine().AppendLine("*Why parents choose us*");
                foreach (var a in about.Where(a => !string.IsNullOrWhiteSpace(a)))
                    sb.AppendLine($"• {a}");
            }

            return sb.AppendLine()
                     .Append("Reply 2 for the fee structure, or 3 for our location.")
                     .ToString();
        }

        private string OfficeAnswer() =>
            "*Talk to the office*\n\n" +
            "Please send your question in this chat and a member of staff will reply " +
            "during school hours, or call:\n" +
            ContactLines();

        private static string Clock(TimeSpan t) =>
            DateTime.Today.Add(t).ToString("hh:mm tt");
    }
}
