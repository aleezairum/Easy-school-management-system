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
            string className, string examName)
        {
            ViewData["Title"] = "Students Position Wise Report";
            ViewData["ClassName"] = className;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}";
            var rows = await FetchAsync<List<PositionRow>>($"{_apiBaseUrl}/examresult/position-report{query}")
                       ?? new List<PositionRow>();

            return View(rows);
        }

        [Route("exam/percentage-report")]
        public async Task<IActionResult> PercentageWiseReport(int classId, int examId,
            string className, string examName)
        {
            ViewData["Title"] = "Students Percentage Wise Report";
            ViewData["ClassName"] = className;
            ViewData["ExamName"] = examName;

            var query = $"?examId={examId}&classId={classId}";
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

        [Route("exam/announcement-report")]
        public async Task<IActionResult> ResultAnnouncement(int examId, string examName)
        {
            ViewData["Title"] = "Result Announcement";
            ViewData["ExamName"] = examName;

            var report = await FetchAsync<AnnouncementPayload>($"{_apiBaseUrl}/examresult/announcement-report?examId={examId}")
                         ?? new AnnouncementPayload();

            return View("ResultAnnouncementReport", report);
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

        // Mirrors the API ExamAnnouncementDto.
        public class AnnouncementPayload
        {
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
        }

        public class AnnouncementTopRow
        {
            public string? ClassName { get; set; }
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
