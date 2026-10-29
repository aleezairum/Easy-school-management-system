using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace School.Web.Controllers
{
    public class ExamController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:5266/api";

        public ExamController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [Route("exam")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Exams";
            return View();
        }

        [Route("exam/form")]
        [Route("exam/form/{id?}")]
        public IActionResult Form(int? id)
        {
            ViewData["Title"] = id.HasValue ? "Edit Exam" : "Add Exam";
            ViewData["ExamId"] = id;
            return View();
        }

        [Route("exam/results/{examId}")]
        public IActionResult Results(int examId)
        {
            ViewData["Title"] = "Exam Results";
            ViewData["ExamId"] = examId;
            return View();
        }

        [Route("exam/marks-entry")]
        [Route("exam/award-list")]
        public IActionResult MarksEntry()
        {
            ViewData["Title"] = "Award List - Marks Entry";
            return View();
        }

        // Students left out of the teachers' result for one exam.
        [Route("exam/exclude-list")]
        public IActionResult ExcludeList()
        {
            ViewData["Title"] = "Result Exclude List";
            return View();
        }

        [Route("exam/reports")]
        public IActionResult Reports()
        {
            ViewData["Title"] = "Exam Reports";
            return View();
        }

        [Route("exam/result-sheet")]
        public async Task<IActionResult> ResultSheet(int classId, int sectionId, int examId, int? prevExamId,
            string className, string sectionName, string examName)
        {
            ViewData["Title"] = "Result Sheet";
            ViewData["ClassName"] = className;
            ViewData["SectionName"] = sectionName;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}&sectionId={sectionId}&prevExamId={prevExamId ?? 0}";
            var sheet = await FetchAsync<ExamResultSheetPayload>($"{_apiBaseUrl}/examresult/result-sheet{query}")
                        ?? new ExamResultSheetPayload();

            return View(sheet);
        }

        [Route("exam/result-card")]
        public async Task<IActionResult> ResultCard(int classId, int sectionId, int examId, int? prevExamId,
            int? studentId, string className, string sectionName, string examName)
        {
            ViewData["Title"] = "Result Card";
            ViewData["ClassName"] = className;
            ViewData["SectionName"] = sectionName;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}&sectionId={sectionId}&prevExamId={prevExamId ?? 0}&studentId={studentId ?? 0}";
            var card = await FetchAsync<ExamResultCardPayload>($"{_apiBaseUrl}/examresult/result-card{query}")
                       ?? new ExamResultCardPayload();

            return View(card);
        }

        [Route("exam/position-report")]
        public async Task<IActionResult> PositionWiseReport(int classId, int examId,
            string className, string examName, int? prevExamId)
        {
            ViewData["Title"] = "Students Position Wise Report";
            ViewData["ClassName"] = className;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}&prevExamId={prevExamId ?? 0}";
            var rows = await FetchAsync<List<PositionRow>>($"{_apiBaseUrl}/examresult/position-report{query}")
                       ?? new List<PositionRow>();

            return View(rows);
        }

        [Route("exam/percentage-report")]
        public async Task<IActionResult> PercentageWiseReport(int classId, int examId,
            string className, string examName, int? prevExamId)
        {
            ViewData["Title"] = "Students Percentage Wise Report";
            ViewData["ClassName"] = className;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}&prevExamId={prevExamId ?? 0}";
            var rows = await FetchAsync<List<PositionRow>>($"{_apiBaseUrl}/examresult/percentage-report{query}")
                       ?? new List<PositionRow>();

            return View(rows);
        }

        [Route("exam/executive-report")]
        public async Task<IActionResult> ExecutiveReport(int classId, int sectionId, int examId,
            string className, string sectionName, string examName)
        {
            ViewData["Title"] = "Executive Report";
            ViewData["ClassName"] = className;
            ViewData["SectionName"] = sectionName;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}&sectionId={sectionId}";
            var report = await FetchAsync<ExecutivePayload>($"{_apiBaseUrl}/examresult/executive-report{query}")
                         ?? new ExecutivePayload();

            return View(report);
        }

        [Route("exam/subject-wise-report")]
        public async Task<IActionResult> SubjectWiseReport(int examId, string examName,
            string? classIds = null, string? sectionIds = null, string? scopeText = null)
        {
            ViewData["Title"] = "Subject Wise Result";

            var query = $"?examId={examId}";
            if (!string.IsNullOrWhiteSpace(classIds))   query += $"&classIds={Uri.EscapeDataString(classIds)}";
            if (!string.IsNullOrWhiteSpace(sectionIds)) query += $"&sectionIds={Uri.EscapeDataString(sectionIds)}";

            var rows = await FetchAsync<List<SubjectWiseRow>>($"{_apiBaseUrl}/examresult/subject-wise-report{query}")
                       ?? new List<SubjectWiseRow>();

            // The report is built SECTION WISE: one table per (Class, Section).
            // Subjects are ranked by Average within their own section.
            var sections = rows
                .GroupBy(r => new { r.ClassId, r.SectionId })
                .Select(g =>
                {
                    var first = g.First();
                    // Rank by the SAME "Average%" shown in the report
                    // (mean of Pass% by Pass Marks and Pass% by Obtained Marks),
                    // so Position matches the percentage column the user sees.
                    var ordered = g.OrderByDescending(r =>
                        ((r.PassPercentByPassMarks ?? 0) + (r.PassPercentByObtainedMarks ?? 0)) / 2m).ToList();
                    var rankById = new Dictionary<int, int>();
                    for (int i = 0; i < ordered.Count; i++)
                        rankById[ordered[i].SubjectId] = i + 1;

                    return new SubjectWiseSection
                    {
                        ClassId     = first.ClassId,
                        SectionId   = first.SectionId,
                        ClassName   = first.ClassName ?? "",
                        SectionName = first.SectionName ?? "",
                        Class       = string.IsNullOrWhiteSpace(first.SectionName)
                                        ? (first.ClassName ?? "")
                                        : $"{first.ClassName} - {first.SectionName}",
                        Subjects    = g.OrderBy(r => r.SubjectName).Select(r => new SubjectResult
                        {
                            Name                       = r.SubjectName ?? "",
                            TotalStudents              = r.TotalStudents,
                            Appeared                   = r.Appeared,
                            Passed                     = r.Passed,
                            Failed                     = r.Failed,
                            PassPercentByPassMarks     = r.PassPercentByPassMarks ?? 0,
                            AggregateTotalMarks        = r.AggregateTotalMarks ?? 0,
                            TotalObtainedMarks         = r.TotalObtainedMarks ?? 0,
                            PassPercentByObtainedMarks = r.PassPercentByObtainedMarks ?? 0,
                            Average                    = r.Average ?? 0,
                            Position                   = Ordinal(rankById.TryGetValue(r.SubjectId, out var p) ? p : 0),
                            TeacherName                = string.IsNullOrWhiteSpace(r.TeacherName) ? "" : r.TeacherName,
                            Signature                  = ""
                        }).ToList()
                    };
                })
                .OrderBy(s => s.ClassId).ThenBy(s => s.SectionId)
                .ToList();

            var model = new SubjectWiseResultViewModel
            {
                SchoolName  = "SIR SYED MODEL HIGH SCHOOL (Boys & Girls)",
                ReportTitle = "Subject Wise Result",
                Examination = examName,
                ScopeText   = string.IsNullOrWhiteSpace(scopeText) ? "Whole School" : scopeText,
                Sections    = sections
            };

            return View("SubjectWiseResult", model);
        }

        [Route("exam/teacher-wise-report")]
        public async Task<IActionResult> TeacherWiseReport(int examId,
            int teacherId, string examName, string? teacherName, bool academy = false)
        {
            ViewData["Title"] = academy ? "Academy Teacher Wise Result" : "Teacher Wise Result";

            // academy = the Academy Subject Allocation and the academy students'
            // results only, in the same report layout.
            var query = $"?examId={examId}&teacherId={teacherId}";
            var endpoint = academy ? "academy-teacher-wise-report" : "teacher-wise-report";
            var rows = await FetchAsync<List<TeacherWiseRow>>($"{_apiBaseUrl}/examresult/{endpoint}{query}")
                       ?? new List<TeacherWiseRow>();

            // Scope label shown in the report header.
            var scope = string.IsNullOrWhiteSpace(teacherName)
                ? (academy ? "All Academy Teachers" : "All Teachers")
                : $"Teacher: {teacherName}";

            // Group the flat (teacher, subject, class, section) rows into one
            // report row per teacher; each cell is a subject result, and the
            // Average % is the mean of that teacher's cell percentages.
            var teachers = rows
                .GroupBy(r => new { r.TeacherId, Name = r.TeacherName ?? "(Unassigned)" })
                .Select(g =>
                {
                    var cells = g
                        .OrderBy(c => c.ClassName).ThenBy(c => c.SubjectName)
                        .Select(c => new TeacherWiseCell
                        {
                            Label   = string.IsNullOrWhiteSpace(c.ColumnLabel) ? c.SubjectName ?? "" : c.ColumnLabel,
                            Percent = c.Percent ?? 0
                        })
                        .ToList();

                    return new TeacherWiseTeacher
                    {
                        TeacherName = g.Key.Name,
                        Cells       = cells,
                        Average     = cells.Count > 0 ? Math.Round(cells.Average(c => c.Percent), 2) : 0
                    };
                })
                // Report is ordered by Position: highest Average first; equal
                // averages fall back to teacher name so the order is stable.
                .OrderByDescending(t => t.Average)
                .ThenBy(t => t.TeacherName)
                .ToList();

            // Rank teachers by Average (highest = 1st). Equal averages share a
            // position; the next distinct average jumps past the tied count.
            decimal? prevAvg = null;
            int rank = 0;
            for (int i = 0; i < teachers.Count; i++)
            {
                if (prevAvg == null || teachers[i].Average != prevAvg)
                    rank = i + 1;
                teachers[i].Position = Ordinal(rank);
                prevAvg = teachers[i].Average;
            }

            var model = new TeacherWiseResultViewModel
            {
                SchoolName  = "SIR SYED MODEL HIGH SCHOOL (Boys & Girls)",
                ReportTitle = academy ? "Academy Teacher Wise Result Report" : "Teacher Wise Result Report",
                Examination = examName,
                Scope       = scope,
                // Fixed number of subject-result columns = the busiest teacher.
                MaxCells    = teachers.Count > 0 ? teachers.Max(t => t.Cells.Count) : 0,
                Teachers    = teachers
            };

            return View("TeacherWiseResult", model);
        }

        // 1 -> "1st", 2 -> "2nd", 11 -> "11th", etc.
        private static string Ordinal(int n)
        {
            if (n <= 0) return "-";
            if (n % 100 is >= 11 and <= 13) return n + "th";
            return n + (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        }

        [Route("exam/teacher-comparison-report")]
        public async Task<IActionResult> TeacherComparisonReport(
            int examId, string examName,
            string? lClassIds = null, string? lSectionIds = null,
            string? rClassIds = null, string? rSectionIds = null,
            string? leftTitle = null, string? rightTitle = null)
        {
            ViewData["Title"] = "Teacher Result Comparison";

            var query = $"?examId={examId}";
            if (!string.IsNullOrWhiteSpace(lClassIds))   query += $"&lClassIds={Uri.EscapeDataString(lClassIds)}";
            if (!string.IsNullOrWhiteSpace(lSectionIds)) query += $"&lSectionIds={Uri.EscapeDataString(lSectionIds)}";
            if (!string.IsNullOrWhiteSpace(rClassIds))   query += $"&rClassIds={Uri.EscapeDataString(rClassIds)}";
            if (!string.IsNullOrWhiteSpace(rSectionIds)) query += $"&rSectionIds={Uri.EscapeDataString(rSectionIds)}";

            var rows = await FetchAsync<List<TeacherComparisonRow>>(
                           $"{_apiBaseUrl}/examresult/teacher-comparison-report{query}")
                       ?? new List<TeacherComparisonRow>();

            var model = new TeacherComparisonViewModel
            {
                Examination = examName,
                LeftTitle   = string.IsNullOrWhiteSpace(leftTitle)  ? "Left"  : leftTitle,
                RightTitle  = string.IsNullOrWhiteSpace(rightTitle) ? "Right" : rightTitle,
                Left  = rows.Where(r => r.Side == "L")
                            .OrderBy(r => r.Sr)
                            .Select(r => new TeacherComparisonRowVm
                            {
                                TeacherName   = r.TeacherName ?? "",
                                ResultPercent = r.ResultPercent ?? 0,
                                Position      = Ordinal(r.Position)
                            }).ToList(),
                Right = rows.Where(r => r.Side == "R")
                            .OrderBy(r => r.Sr)
                            .Select(r => new TeacherComparisonRowVm
                            {
                                TeacherName   = r.TeacherName ?? "",
                                ResultPercent = r.ResultPercent ?? 0,
                                Position      = Ordinal(r.Position)
                            }).ToList()
            };

            return View("TeacherComparisonReport", model);
        }

        [Route("exam/announcement-report")]
        public async Task<IActionResult> ResultAnnouncement(
            int examId, string examName,
            string? classIds = null, string? sectionIds = null,
            string? scopeText = null, int? prevExamId = null)
        {
            ViewData["Title"] = "Result Announcement";
            ViewData["ExamName"] = examName;
            ViewData["ScopeText"] = string.IsNullOrWhiteSpace(scopeText) ? "Whole School" : scopeText;

            var url = $"{_apiBaseUrl}/examresult/announcement-report?examId={examId}";
            if (!string.IsNullOrWhiteSpace(classIds))   url += $"&classIds={Uri.EscapeDataString(classIds)}";
            if (!string.IsNullOrWhiteSpace(sectionIds)) url += $"&sectionIds={Uri.EscapeDataString(sectionIds)}";
            // Left out: the API compares with the previous term itself. 0: no comparison.
            if (prevExamId.HasValue) url += $"&prevExamId={prevExamId.Value}";

            var report = await FetchAsync<AnnouncementPayload>(url) ?? new AnnouncementPayload();

            return View("ResultAnnouncementReport", report);
        }

        /// <summary>
        /// Comparison Result Sheet: each class's result % exam by exam (normally the
        /// session's 1st / 2nd / 3rd Term), for the girls' sections, the boys' sections
        /// and both together, as the school prepared it by hand.
        /// </summary>
        [Route("exam/comparison-sheet")]
        public async Task<IActionResult> ComparisonSheet(int sessionId, string? examIds, string? sessionName)
        {
            ViewData["Title"] = "Comparison Result Sheet";
            var ids = (examIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => int.TryParse(s, out var v) ? v : 0).Where(v => v > 0).ToHashSet();
            var exams = (await FetchAsync<List<ComparisonExam>>($"{_apiBaseUrl}/examresult/comparison-exams?sessionId={sessionId}") ?? new())
                        .Where(e => ids.Contains(e.ExamId)).OrderBy(e => e.FirstPaper).ToList();
            var rows = exams.Count == 0 ? new List<ComparisonRow>()
                : await FetchAsync<List<ComparisonRow>>($"{_apiBaseUrl}/examresult/comparison?examIds={string.Join(",", exams.Select(e => e.ExamId))}") ?? new();
            return View("ComparisonResultSheet", ComparisonSheetViewModel.Build(exams, rows, sessionName));
        }

        public class ComparisonExam
        {
            public int ExamId { get; set; }
            public string ExamName { get; set; } = "";
            public DateTime FirstPaper { get; set; }
            public bool IsTerm { get; set; }
        }

        public class ComparisonRow
        {
            public int ExamId { get; set; }
            public int ClassId { get; set; }
            public string ClassName { get; set; } = "";
            public string Side { get; set; } = "";
            public int Total { get; set; }
            public int Passed { get; set; }
        }

        public class ComparisonLine
        {
            public string ClassName { get; set; } = "";
            public List<decimal?> Values { get; set; } = new();   // one per exam; null = no result ("Nill")
            public decimal? Overall { get; set; }
        }

        public class ComparisonTable
        {
            public string Title { get; set; } = "";
            public string Section { get; set; } = "";             // G, B or B+G
            public List<ComparisonLine> Lines { get; set; } = new();
            public List<decimal?> Average { get; set; } = new();
            public decimal? OverallAverage { get; set; }
        }

        public class ComparisonSheetViewModel
        {
            public string SessionLabel { get; set; } = "";
            public List<string> ExamNames { get; set; } = new();
            public ComparisonTable Girls { get; set; } = new();
            public ComparisonTable Boys { get; set; } = new();
            public ComparisonTable Both { get; set; } = new();

            private static decimal? Avg(IEnumerable<decimal?> v)
            {
                var have = v.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                return have.Count == 0 ? null : have.Average();
            }

            // Play Group, Nursery, Prep first, then 1, 2 ... 10 by number, a "9-Junior"
            // just before 9; anything else (Montessori) by its position in the list.
            private static double ClassOrder(string name, int id)
            {
                var n = name.Trim().ToLowerInvariant();
                if (n.StartsWith("play")) return 1;
                if (n.StartsWith("nursery")) return 2;
                if (n.StartsWith("prep")) return 3;
                var m = System.Text.RegularExpressions.Regex.Match(n, @"^\d+");
                if (m.Success) return 10 + int.Parse(m.Value) * 2 - (n.Contains("junior") ? 1 : 0);
                return id < 10 ? 0.5 : 100 + id;
            }

            // "1st Term 2026-27" -> "1st Term"; "Schedule Test No.1 (19-06-2026 to ...)" -> "Schedule Test No.1"
            private static string ShortName(string name) =>
                System.Text.RegularExpressions.Regex.Replace(
                    System.Text.RegularExpressions.Regex.Replace(name, @"\([^)]*\)", ""), @"\s*\d{4}\s*-\s*\d{2,4}\s*$", "").Trim();

            public static ComparisonSheetViewModel Build(List<ComparisonExam> exams, List<ComparisonRow> rows, string? sessionName)
            {
                var vm = new ComparisonSheetViewModel
                {
                    SessionLabel = System.Text.RegularExpressions.Regex.Replace(sessionName ?? "", @"^\s*year\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim(),
                    ExamNames = exams.Select(e => ShortName(e.ExamName)).ToList()
                };
                rows = rows.Where(r => r.Side == "B" || r.Side == "G").ToList();
                var classes = rows.GroupBy(r => r.ClassId)
                                  .Select(g => (Id: g.Key, Name: g.First().ClassName))
                                  .OrderBy(c => ClassOrder(c.Name, c.Id)).ThenBy(c => c.Id).ToList();

                decimal? Pct(int examId, int classId, string side)
                {
                    var r = rows.FirstOrDefault(x => x.ExamId == examId && x.ClassId == classId && x.Side == side);
                    return r == null || r.Total == 0 ? null : r.Passed * 100m / r.Total;
                }

                ComparisonTable Table(string title, string section, Func<int, int, decimal?> value)
                {
                    var t = new ComparisonTable { Title = title, Section = section };
                    foreach (var c in classes)
                    {
                        var values = exams.Select(e => value(e.ExamId, c.Id)).ToList();
                        if (values.All(v => v == null)) continue;           // no such sections in this class
                        t.Lines.Add(new ComparisonLine { ClassName = c.Name, Values = values, Overall = Avg(values) });
                    }
                    t.Average = exams.Select((_, i) => Avg(t.Lines.Select(l => l.Values[i]))).ToList();
                    t.OverallAverage = Avg(t.Lines.Select(l => l.Overall));
                    return t;
                }

                vm.Girls = Table("Comparison Result Sheet", "G", (e, c) => Pct(e, c, "G"));
                vm.Boys = Table("Comparison Result Sheet", "B", (e, c) => Pct(e, c, "B"));
                // Both together: the average of the boys' and the girls' result, as on the school's sheet.
                vm.Both = Table("Overall Comparison Result Sheet", "B+G", (e, c) => Avg(new[] { Pct(e, c, "B"), Pct(e, c, "G") }));
                return vm;
            }
        }

        // ── helpers ──────────────────────────────────────────────────────
        private async Task<T?> FetchAsync<T>(string url) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        // Local payload mirroring the API ExamResultSheetDto (so we don't take a
        // hard reference on School.API).
        public class ExamResultSheetPayload
        {
            public List<ResultSheetSubject> Subjects { get; set; } = new();
            public List<ResultSheetStudent> Students { get; set; } = new();
        }

        public class ResultSheetSubject
        {
            public int SubjectId { get; set; }
            public string? SubjectName { get; set; }
            public decimal? TotalMarks { get; set; }
            public decimal? PassMarks { get; set; }
        }

        public class ResultSheetStudent
        {
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? FatherName { get; set; }
            public int? Position { get; set; }
            public decimal? CurrentPct { get; set; }
            public decimal? PrevPct { get; set; }
            public string? Result { get; set; }
            public decimal? Total { get; set; }
            public Dictionary<string, decimal?> Marks { get; set; } = new();
        }

        // Local payload mirroring the API ExamResultCardDto.
        public class ExamResultCardPayload
        {
            public List<ResultCardStudent> Students { get; set; } = new();
        }

        public class ResultCardStudent
        {
            public int StudentId { get; set; }
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? FatherName { get; set; }
            public string? ClassName { get; set; }
            public string? SectionName { get; set; }
            public int? Position { get; set; }
            public int TotalStudents { get; set; }
            public decimal? HighestMarks { get; set; }
            public decimal? CurrentPct { get; set; }
            public decimal? PrevPct { get; set; }
            public decimal? GrandTotal { get; set; }
            public decimal? GrandPass { get; set; }
            public decimal? GrandObt { get; set; }
            public string? GrandStatus { get; set; }
            public int PresentDays { get; set; }
            public int SchoolDays { get; set; }
            public decimal? AttendancePct { get; set; }
            public int AcademyPresentDays { get; set; }
            public int AcademyDays { get; set; }
            public decimal? AcademyPct { get; set; }
            public List<ResultCardSubject> Subjects { get; set; } = new();
        }

        public class ResultCardSubject
        {
            public string? SubjectName { get; set; }
            public DateTime? VDate { get; set; }
            public decimal? TotalMarks { get; set; }
            public decimal? PassMarks { get; set; }
            public decimal? MarksObtained { get; set; }
            public string? Status { get; set; }
        }

        // Mirrors the API ExamPositionRowDto.
        public class PositionRow
        {
            public int Sr { get; set; }
            public int? Position { get; set; }
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? FatherName { get; set; }
            public string? SectionName { get; set; }
            public decimal? TotalMarks { get; set; }
            public decimal? ObtainedMarks { get; set; }
            public decimal? Percent { get; set; }
            public string? Status { get; set; }
            public decimal? PrevPct { get; set; }
        }

        // Mirrors the API ExamExecutiveDto.
        public class ExecutivePayload
        {
            public ExecutiveOverall Overall { get; set; } = new();
            public List<ExecutiveSubject> Subjects { get; set; } = new();
        }

        public class ExecutiveOverall
        {
            public int TotalStudents { get; set; }
            public int TotalPass { get; set; }
            public int TotalFail { get; set; }
            public decimal? ResultPct { get; set; }
        }

        public class ExecutiveSubject
        {
            public string? SubjectName { get; set; }
            public int FailCount { get; set; }
            public decimal? FailPct { get; set; }
            public int PassCount { get; set; }
            public decimal? PassPct { get; set; }
            public string? TeacherName { get; set; }
            public decimal? Obtained { get; set; }
            public decimal? Total { get; set; }
            public decimal? Percent { get; set; }
        }

        // Mirrors the API ExamSubjectWiseRowDto.
        public class SubjectWiseRow
        {
            public int ClassId { get; set; }
            public string? ClassName { get; set; }
            public int SectionId { get; set; }
            public string? SectionName { get; set; }
            public int SubjectId { get; set; }
            public string? SubjectName { get; set; }
            public int TotalStudents { get; set; }
            public int Appeared { get; set; }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public decimal? PassPercentByPassMarks { get; set; }
            public decimal? AggregateTotalMarks { get; set; }
            public decimal? TotalObtainedMarks { get; set; }
            public decimal? PassPercentByObtainedMarks { get; set; }
            public decimal? Average { get; set; }
            public string? TeacherName { get; set; }
        }

        // View model for the SubjectWiseResult report view (section wise — one
        // table per section, two sections per printed page).
        public class SubjectWiseResultViewModel
        {
            public string SchoolName { get; set; } = "";
            public string ReportTitle { get; set; } = "";
            public string Examination { get; set; } = "";
            public string ScopeText { get; set; } = "";
            public List<SubjectWiseSection> Sections { get; set; } = new();
        }

        // One section's subject-wise block.
        public class SubjectWiseSection
        {
            public int ClassId { get; set; }
            public int SectionId { get; set; }
            public string ClassName { get; set; } = "";
            public string SectionName { get; set; } = "";
            public string Class { get; set; } = "";   // "Class - Section" label for the meta row
            public List<SubjectResult> Subjects { get; set; } = new();
        }

        public class SubjectResult
        {
            public string Name { get; set; } = "";
            public int TotalStudents { get; set; }
            public int Appeared { get; set; }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public decimal PassPercentByPassMarks { get; set; }
            public decimal AggregateTotalMarks { get; set; }
            public decimal TotalObtainedMarks { get; set; }
            public decimal PassPercentByObtainedMarks { get; set; }
            public decimal Average { get; set; }
            public string Position { get; set; } = "";
            public string TeacherName { get; set; } = "";
            public string Signature { get; set; } = "";
        }

        // Mirrors the API ExamTeacherWiseRowDto (flat: one per teacher+subject+class+section).
        public class TeacherWiseRow
        {
            public int? TeacherId { get; set; }
            public string? TeacherName { get; set; }
            public int SubjectId { get; set; }
            public string? SubjectName { get; set; }
            public int ClassId { get; set; }
            public string? ClassName { get; set; }
            public int SectionId { get; set; }
            public string? SectionName { get; set; }
            public string? ColumnLabel { get; set; }
            public int Appeared { get; set; }
            public decimal? TotalMarks { get; set; }
            public decimal? ObtainedMarks { get; set; }
            public decimal? Percent { get; set; }
        }

        // View model for the TeacherWiseResult report view.
        public class TeacherWiseResultViewModel
        {
            public string SchoolName { get; set; } = "";
            public string ReportTitle { get; set; } = "";
            public string Examination { get; set; } = "";
            public string Scope { get; set; } = "";
            public int MaxCells { get; set; }
            public List<TeacherWiseTeacher> Teachers { get; set; } = new();
        }

        public class TeacherWiseTeacher
        {
            public string TeacherName { get; set; } = "";
            public List<TeacherWiseCell> Cells { get; set; } = new();
            public decimal Average { get; set; }
            public string Position { get; set; } = "";
        }

        public class TeacherWiseCell
        {
            public string Label { get; set; } = "";
            public decimal Percent { get; set; }
        }

        // Mirrors the API ExamTeacherComparisonRowDto (one ranked teacher per scope).
        public class TeacherComparisonRow
        {
            public string Side { get; set; } = "L";
            public int Sr { get; set; }
            public int? TeacherId { get; set; }
            public string? TeacherName { get; set; }
            public decimal? ResultPercent { get; set; }
            public int Position { get; set; }
        }

        // View model for the side-by-side TeacherComparisonReport view.
        public class TeacherComparisonViewModel
        {
            public string Examination { get; set; } = "";
            public string LeftTitle  { get; set; } = "Left";
            public string RightTitle { get; set; } = "Right";
            public List<TeacherComparisonRowVm> Left  { get; set; } = new();
            public List<TeacherComparisonRowVm> Right { get; set; } = new();
        }

        public class TeacherComparisonRowVm
        {
            public string TeacherName { get; set; } = "";
            public decimal ResultPercent { get; set; }
            public string Position { get; set; } = "";
        }

        // Mirrors the API ExamAnnouncementDto.
        public class AnnouncementPayload
        {
            public int? PrevExamId { get; set; }
            public string? PrevExamName { get; set; }
            public ResultStatRow Overall { get; set; } = new();
            public List<ResultStatRow> Campuses { get; set; } = new();
            public List<ResultStatRow> Classes { get; set; } = new();
            public List<AnnouncementTopRow> TopPositions { get; set; } = new();
        }

        public class ResultStatRow
        {
            public string? Name { get; set; }
            public int TotalStudents { get; set; }
            public int TotalPass { get; set; }
            public int TotalFail { get; set; }
            public decimal? PassPct { get; set; }
            public decimal? FailPct { get; set; }
            public decimal? PrevPassPct { get; set; }
        }

        public class AnnouncementTopRow
        {
            public string? ClassName { get; set; }
            public string? Scope { get; set; }       // "Overall" | "Girls" | "Boys"
            public int ScopeOrder { get; set; }       // 0 = Overall, 1 = Girls, 2 = Boys
            public int? Position { get; set; }
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? FatherName { get; set; }
            public string? SectionName { get; set; }
            public decimal? TotalMarks { get; set; }
            public decimal? ObtainedMarks { get; set; }
            public decimal? Percent { get; set; }
        }
    }
}
