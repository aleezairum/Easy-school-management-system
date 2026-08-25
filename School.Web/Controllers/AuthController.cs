using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace School.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [Route("login")]
        [Route("")]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Dashboard");

            // Render instantly. The Academic Session dropdown is populated client-side
            // (non-blocking fetch in the view) so a slow/cold API never delays first
            // paint of the login form. Login does not require the list to be present.
            return View();
        }

        private async Task<List<SessionItem>> GetSessionsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var apiBase = _configuration["ApiBaseUrl"] ?? "http://localhost:5266/api";

                var body = await client.GetStringAsync($"{apiBase}/AcademicSessionYear");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<SessionItem>>(body, options) ?? new List<SessionItem>();
            }
            catch
            {
                return new List<SessionItem>();
            }
        }

        public class SessionItem
        {
            public int VID { get; set; }
            public string VName { get; set; } = "";
            public bool IsCurrent { get; set; }
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(string username, string password, int session)
        {
            // Academic Session is mandatory.
            if (session <= 0)
            {
                ViewBag.Error = "Please select an Academic Session";
                ViewBag.Sessions = await GetSessionsAsync();
                return View();
            }

            var client = _httpClientFactory.CreateClient();
            var apiBase = _configuration["ApiBaseUrl"] ?? "http://localhost:5266/api";

            var payload = new { UserLogin = username, UserPassword = password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{apiBase}/SecUser/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(body);

                    int userId = result.GetProperty("vid").GetInt32();
                    string fullName = result.GetProperty("userFullName").GetString() ?? "";

                    HttpContext.Session.SetInt32("UserId", userId);
                    HttpContext.Session.SetString("UserFullName", fullName);
                    HttpContext.Session.SetInt32("SessionId", session);

                    return RedirectToAction("Index", "Dashboard");
                }
            }
            catch
            {
                // API not reachable
            }

            ViewBag.Error = "Invalid username or password";
            ViewBag.Sessions = await GetSessionsAsync();
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
