using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;

namespace School.Web.Controllers
{
    public class CertificatesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:5266/api";

        public CertificatesController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [Route("certificates")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Certificates";
            return View();
        }

        [Route("certificates/leaving/{id}")]
        public async Task<IActionResult> Leaving(
            int id,
            DateTime? leavingDate = null,
            string? examination = null,
            string? marksObtained = null,
            string? totalMarks = null)
        {
            ViewData["Title"] = "School Leaving Certificate";

            var query = BuildQuery(new Dictionary<string, string?>
            {
                ["leavingDate"]   = leavingDate?.ToString("yyyy-MM-dd"),
                ["examination"]   = examination,
                ["marksObtained"] = marksObtained,
                ["totalMarks"]    = totalMarks
            });

            var data = await FetchAsync<LeavingCertificatePayload>($"{_apiBaseUrl}/Certificate/leaving/{id}{query}");
            if (data == null) return NotFound("Student not found.");

            return View("SchoolLeavingCertificate", ToDict(data));
        }

        [Route("certificates/character/{id}")]
        public async Task<IActionResult> Character(
            int id,
            int? sessionId = null,
            string? boardRollNo = null,
            string? examRollNo = null,
            string? examination = null,
            string? marksObtained = null,
            string? totalMarks = null,
            string? conduct = null,
            string? moralCharacter = null,
            DateTime? issueDate = null)
        {
            ViewData["Title"] = "Provisional & Character Certificate";

            var query = BuildQuery(new Dictionary<string, string?>
            {
                ["sessionId"]      = sessionId?.ToString(),
                ["boardRollNo"]    = boardRollNo,
                ["examRollNo"]     = examRollNo,
                ["examination"]    = examination,
                ["marksObtained"]  = marksObtained,
                ["totalMarks"]     = totalMarks,
                ["conduct"]        = conduct,
                ["moralCharacter"] = moralCharacter,
                ["issueDate"]      = issueDate?.ToString("yyyy-MM-dd")
            });

            var data = await FetchAsync<ProvisionalCertificatePayload>($"{_apiBaseUrl}/Certificate/provisional/{id}{query}");
            if (data == null) return NotFound("Student not found.");

            return View("ProvisionalCharacterCertificate", ToDict(data));
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

        private static string BuildQuery(Dictionary<string, string?> parts)
        {
            var pairs = parts
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}")
                .ToList();
            return pairs.Count == 0 ? "" : "?" + string.Join("&", pairs);
        }

        // Build a dictionary keyed by PascalCase property names so the views'
        // ((IDictionary<string,object>)Model)[key] accessor finds them.
        private static IDictionary<string, object?> ToDict<T>(T obj) where T : class
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                dict[prop.Name] = prop.GetValue(obj);
            }
            return dict;
        }

        // Local payload classes mirror the API DTOs so we don't take a hard
        // reference on School.API.
        private class LeavingCertificatePayload
        {
            public int VID { get; set; }
            public int? CampusID { get; set; }
            public string? AdmissionNo { get; set; }
            public DateTime? AdmissionDate { get; set; }
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? Name_Urdu { get; set; }
            public DateTime? DOB { get; set; }
            public string? Gender { get; set; }
            public string? Address { get; set; }
            public string? FatherName { get; set; }
            public string? FatherNameUrdu { get; set; }
            public string? FatherCNIC { get; set; }
            public string? FatherMobile { get; set; }
            public string? ClassAdmitted { get; set; }
            public string? ClassStudying { get; set; }
            public DateTime? LeavingDate { get; set; }
            public string? Examination { get; set; }
            public string? MarksObtained { get; set; }
            public string? TotalMarks { get; set; }
        }

        private class ProvisionalCertificatePayload
        {
            public int VID { get; set; }
            public int? CampusID { get; set; }
            public string? RegistrationNo { get; set; }
            public string? AdmissionNo { get; set; }
            public DateTime? AdmissionDate { get; set; }
            public string? RollNo { get; set; }
            public string? StudentName { get; set; }
            public string? Name_Urdu { get; set; }
            public DateTime? DOB { get; set; }
            public string? Gender { get; set; }
            public string? Address { get; set; }
            public string? FatherName { get; set; }
            public string? FatherNameUrdu { get; set; }
            public string? FatherCNIC { get; set; }
            public string? SessionName { get; set; }
            public string? BoardRollNo { get; set; }
            public string? ExamRollNo { get; set; }
            public string? Examination { get; set; }
            public string? MarksObtained { get; set; }
            public string? TotalMarks { get; set; }
            public string? Conduct { get; set; }
            public string? MoralCharacter { get; set; }
            public DateTime? IssueDate { get; set; }
        }
    }
}
