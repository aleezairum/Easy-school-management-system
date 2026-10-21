using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using School.API.DTOs.Settings;
using School.API.Extensions;
using School.API.Repositories.Interfaces.Settings;
using School.API.Services.Interfaces.Settings;

namespace School.API.Controllers.Settings
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupJobController : ControllerBase
    {
        private readonly IBackupJobSettingService _service;
        private readonly IBackupJobSettingRepository _repo;
        private readonly IGoogleOAuthConnectService _oauthService;
        private readonly IMemoryCache _cache;

        public BackupJobController(
            IBackupJobSettingService service,
            IBackupJobSettingRepository repo,
            IGoogleOAuthConnectService oauthService,
            IMemoryCache cache)
        {
            _service = service;
            _repo = repo;
            _oauthService = oauthService;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var record = await _service.GetAsync();
            return record == null ? NotFound() : Ok(record);
        }

        [HttpPost]
        public async Task<IActionResult> Save(BackupJobSettingSaveDto dto)
        {
            int userId = Request.GetUserId();
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _service.SaveAsync(dto, userId, userIp);
            return Ok(result);
        }

        [HttpPost("run-now")]
        public async Task<IActionResult> RunNow(CancellationToken ct)
        {
            var result = await _service.RunBackupNowAsync(ct);
            return Ok(result);
        }

        [HttpGet("local-files")]
        public async Task<IActionResult> LocalFiles()
        {
            var files = await _service.GetLocalBackupFilesAsync();
            return Ok(files);
        }

        [HttpGet("available-databases")]
        public IActionResult AvailableDatabases()
            => Ok(_service.GetAvailableDatabases());

        // ── Google Drive "Connect with Google" (OAuth SSO-style) flow ──────

        [HttpGet("oauth/start")]
        public async Task<IActionResult> OAuthStart()
        {
            var settings = await _service.GetAsync();
            if (settings == null || string.IsNullOrWhiteSpace(settings.OAuthClientId))
            {
                return BadRequest("Save the OAuth Client ID (and Client Secret) first, then click Connect.");
            }

            // The saved settings never carry the client secret back to the browser,
            // so re-read it directly where it's still populated (server-side only).
            var clientSecret = await GetClientSecretAsync();
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                return BadRequest("Save the OAuth Client Secret first, then click Connect.");
            }

            var state = Guid.NewGuid().ToString("N");
            _cache.Set($"oauth-state-{state}", true, TimeSpan.FromMinutes(10));

            var redirectUri = BuildRedirectUri();
            var authUrl = _oauthService.BuildAuthorizationUrl(settings.OAuthClientId!, clientSecret, redirectUri, state);
            return Redirect(authUrl);
        }

        // Google redirects the browser here; the one-time state in the cache is the guard.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpGet("oauth/callback")]
        public async Task<IActionResult> OAuthCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                return HtmlResult(false, $"Google reported: {error}");

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return HtmlResult(false, "Missing authorization code.");

            var cacheKey = $"oauth-state-{state}";
            if (!_cache.TryGetValue(cacheKey, out _))
                return HtmlResult(false, "This connection link has expired or was already used. Please try Connect again.");
            _cache.Remove(cacheKey);

            var settings = await _service.GetAsync();
            var clientSecret = await GetClientSecretAsync();
            if (settings == null || string.IsNullOrWhiteSpace(settings.OAuthClientId) || string.IsNullOrWhiteSpace(clientSecret))
                return HtmlResult(false, "OAuth Client ID/Secret are not configured.");

            var redirectUri = BuildRedirectUri();
            var exchange = await _oauthService.ExchangeCodeForRefreshTokenAsync(
                settings.OAuthClientId!, clientSecret, redirectUri, code, HttpContext.RequestAborted);

            if (!exchange.Success)
            {
                // Show Google's own reason — a generic message here used to make
                // a wrong Client Secret look identical to a blocked firewall.
                return HtmlResult(false, $"{exchange.Error} (redirect URI used: {redirectUri})");
            }

            await _service.SaveOAuthRefreshTokenAsync(exchange.RefreshToken!);
            return HtmlResult(true, "Google Drive connected successfully.");
        }

        // GetAsync() (the public DTO) never carries the client secret back to the
        // browser, so read it directly from the repository for this server-side use.
        private async Task<string?> GetClientSecretAsync()
        {
            var raw = await _repo.GetAsync();
            return raw?.OAuthClientSecret;
        }

        private string BuildRedirectUri() => $"{Request.Scheme}://{Request.Host}/api/BackupJob/oauth/callback";

        private ContentResult HtmlResult(bool success, string message)
        {
            var color = success ? "#15803d" : "#b91c1c";
            var icon = success ? "&#10003;" : "&#10005;";
            var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>Google Drive Connection</title></head>
<body style='font-family:Arial,sans-serif;text-align:center;padding:60px 20px;'>
  <div style='font-size:48px;color:{color};'>{icon}</div>
  <h3 style='color:#1e293b;'>{(success ? "Connected" : "Connection Failed")}</h3>
  <p style='color:#64748b;max-width:420px;margin:0 auto;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
  <p style='color:#94a3b8;font-size:13px;margin-top:24px;'>You can close this tab and return to the Backup settings page.</p>
  <script>setTimeout(function(){{ window.close(); }}, 4000);</script>
</body></html>";
            return Content(html, "text/html");
        }
    }
}
