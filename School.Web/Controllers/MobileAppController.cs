using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace School.Web.Controllers
{
    /// <summary>
    /// The office's pages for the EasySchool mobile app, and the public page
    /// parents and staff download the Android app from (no sign-in - the
    /// session guard in Program.cs lets "app", "app/download" and "app/qr.png" through).
    ///
    /// The APK is not in the Play Store: it is copied to MobileApp:ApkFile on
    /// the server (default AppDownloads/EasySchool.apk next to the site) and
    /// served from here. The app itself asks api/app/school-info for
    /// App:LatestVersion and offers this page when a newer one is up.
    /// </summary>
    public class MobileAppController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _http;

        public MobileAppController(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory http)
        {
            _config = config;
            _env = env;
            _http = http;
        }

        [Route("app/logins")]
        public IActionResult Logins()
        {
            ViewData["Title"] = "App Logins";
            return View();
        }

        [Route("app/usage")]
        public IActionResult Usage()
        {
            ViewData["Title"] = "App Usage";
            return View();
        }

        [Route("app/permissions")]
        public IActionResult Permissions()
        {
            ViewData["Title"] = "Mobile App Permissions";
            return View();
        }

        private string ApkPath()
        {
            var p = _config["MobileApp:ApkFile"] ?? "AppDownloads/EasySchool.apk";
            return Path.IsPathRooted(p) ? p : Path.Combine(_env.ContentRootPath, p);
        }

        /// <summary>The address printed on the login slips and in the QR code.</summary>
        public static string DownloadUrl(IConfiguration config) =>
            config["MobileApp:DownloadUrl"] ?? "https://school.easypos.com.pk/EasySchool/app";

        public class DownloadPageModel
        {
            public string SchoolName { get; set; } = "EasySchool";
            public string? Version { get; set; }
            public bool Available { get; set; }
            public double SizeMb { get; set; }
            public DateTime? Updated { get; set; }
            public string DownloadUrl { get; set; } = "";
        }

        [Route("app")]
        public async Task<IActionResult> Download()
        {
            var file = new FileInfo(ApkPath());
            var model = new DownloadPageModel
            {
                Available = file.Exists,
                SizeMb = file.Exists ? Math.Round(file.Length / 1048576.0, 1) : 0,
                Updated = file.Exists ? file.LastWriteTime : null,
                DownloadUrl = DownloadUrl(_config)
            };
            // The school's name and the version on offer come from the API (App:LatestVersion).
            try
            {
                var client = _http.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(4);
                var apiBase = _config["ApiBaseUrl"] ?? "http://localhost:5266/api";
                var j = JsonSerializer.Deserialize<JsonElement>(await client.GetStringAsync($"{apiBase}/app/school-info"));
                if (j.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } name) model.SchoolName = name;
                if (j.TryGetProperty("latestVersion", out var v)) model.Version = v.GetString();
            }
            catch { }
            return View(model);
        }

        [Route("app/download")]
        public IActionResult DownloadApk()
        {
            var path = ApkPath();
            if (!System.IO.File.Exists(path)) return NotFound("The app has not been uploaded to the server yet.");
            return PhysicalFile(path, "application/vnd.android.package-archive", "EasySchool.apk", enableRangeProcessing: true);
        }

        /// <summary>QR code of the download page, for the login slips.</summary>
        [Route("app/qr.png")]
        [ResponseCache(Duration = 86400)]
        public IActionResult Qr()
        {
            using var gen = new QRCoder.QRCodeGenerator();
            using var data = gen.CreateQrCode(DownloadUrl(_config), QRCoder.QRCodeGenerator.ECCLevel.M);
            var png = new QRCoder.PngByteQRCode(data).GetGraphic(10);
            return File(png, "image/png");
        }
    }
}
