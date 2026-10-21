using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace School.API.Services.Implementations.MobileApp
{
    /// <summary>
    /// The push topics a phone subscribes to. One EasySchool app serves every
    /// school, so each name starts with the school's code (App:SchoolCode):
    ///   es_SSMHS_parents      everyone in parent / student mode
    ///   es_SSMHS_c2_parents   ... with a child at campus 2
    ///   es_SSMHS_staff        all staff
    ///   es_SSMHS_c2_staff     staff of campus 2
    /// The same rule as the in-app announcement list (AppService.AnnouncementsAsync),
    /// so a push never arrives for an announcement the app will not show.
    /// </summary>
    public static class AppTopics
    {
        public static string Prefix(IConfiguration config)
        {
            var code = new string((config["App:SchoolCode"] ?? "school").Where(char.IsLetterOrDigit).ToArray());
            return "es_" + (code.Length == 0 ? "school" : code);
        }

        public static List<string> ForParent(string prefix, IEnumerable<int> campusIds) =>
            new[] { $"{prefix}_parents" }.Concat(campusIds.Distinct().OrderBy(c => c).Select(c => $"{prefix}_c{c}_parents")).ToList();

        public static List<string> ForStaff(string prefix, int? campusId) =>
            campusId is int c ? new() { $"{prefix}_staff", $"{prefix}_c{c}_staff" } : new() { $"{prefix}_staff" };

        /// <summary>Audience: 0 everyone, 1 parents, 2 staff. Campus null: both campuses.</summary>
        public static List<string> ForAnnouncement(string prefix, byte audience, int? campusId)
        {
            var groups = audience switch { 1 => new[] { "parents" }, 2 => new[] { "staff" }, _ => new[] { "parents", "staff" } };
            return groups.Select(g => campusId is int c ? $"{prefix}_c{c}_{g}" : $"{prefix}_{g}").ToList();
        }
    }

    /// <summary>Wakes the announcement push job as soon as an announcement is saved.</summary>
    public sealed class AppPushSignal
    {
        private readonly SemaphoreSlim _signal = new(0, 1);
        public void Poke() { try { _signal.Release(); } catch (SemaphoreFullException) { } }
        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);
    }

    /// <summary>The outcome of one send: Retry = worth trying again later (network, 5xx, 429).</summary>
    public sealed record FcmResult(bool Ok, bool Retry, string? Detail);

    /// <summary>
    /// Firebase Cloud Messaging, HTTP v1. Signs its own Google access token from
    /// the Firebase service-account file (no Google SDK needed) and keeps it for
    /// 50 minutes.
    ///   Firebase:Enabled             false: nothing is sent
    ///   Firebase:ServiceAccountFile  the JSON downloaded from Firebase console ->
    ///                                Project settings -> Service accounts
    ///   Firebase:ProjectId           optional; the file's project_id otherwise
    /// </summary>
    public sealed class FcmSender
    {
        private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly IConfiguration _config;
        private readonly ILogger<FcmSender> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private string? _accessToken;
        private DateTime _expiresAt;

        public FcmSender(IConfiguration config, ILogger<FcmSender> logger) { _config = config; _logger = logger; }

        public bool Enabled => _config.GetValue("Firebase:Enabled", false) && !string.IsNullOrWhiteSpace(_config["Firebase:ServiceAccountFile"]);

        private sealed record Account(string ProjectId, string ClientEmail, string PrivateKey, string TokenUri);

        private Account LoadAccount()
        {
            var path = _config["Firebase:ServiceAccountFile"]!;
            if (!Path.IsPathRooted(path)) path = Path.Combine(AppContext.BaseDirectory, path);
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            return new Account(
                _config["Firebase:ProjectId"] is { Length: > 0 } p ? p : (string)json["project_id"]!,
                (string)json["client_email"]!,
                (string)json["private_key"]!,
                (string?)json["token_uri"] ?? "https://oauth2.googleapis.com/token");
        }

        private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static string B64(string s) => B64(Encoding.UTF8.GetBytes(s));

        private async Task<(string Token, string ProjectId)> AccessTokenAsync(CancellationToken ct)
        {
            var acc = LoadAccount();
            await _lock.WaitAsync(ct);
            try
            {
                if (_accessToken != null && DateTime.UtcNow < _expiresAt) return (_accessToken, acc.ProjectId);

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var header = B64(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" }));
                var claims = B64(JsonSerializer.Serialize(new { iss = acc.ClientEmail, scope = Scope, aud = acc.TokenUri, iat = now, exp = now + 3600 }));
                using var rsa = RSA.Create();
                rsa.ImportFromPem(acc.PrivateKey);
                var signature = B64(rsa.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

                using var res = await Http.PostAsync(acc.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = $"{header}.{claims}.{signature}"
                }), ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Google token refused ({(int)res.StatusCode}): {body}");
                _accessToken = (string)JsonNode.Parse(body)!["access_token"]!;
                _expiresAt = DateTime.UtcNow.AddMinutes(50);
                return (_accessToken, acc.ProjectId);
            }
            finally { _lock.Release(); }
        }

        /// <summary>One notification to every phone on any of the topics (at most 5).</summary>
        public async Task<FcmResult> SendToTopicsAsync(IReadOnlyList<string> topics, string title, string body,
            IDictionary<string, string> data, string? imageUrl, CancellationToken ct)
        {
            if (!Enabled) return new(false, false, "Firebase is not enabled.");
            if (topics.Count is 0 or > 5) return new(false, false, "1 to 5 topics.");
            try
            {
                var (token, projectId) = await AccessTokenAsync(ct);

                var notification = new JsonObject { ["title"] = title, ["body"] = body };
                if (!string.IsNullOrEmpty(imageUrl)) notification["image"] = imageUrl;
                var message = new JsonObject
                {
                    ["notification"] = notification,
                    ["data"] = new JsonObject(data.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)JsonValue.Create(kv.Value)))),
                    ["android"] = new JsonObject
                    {
                        ["priority"] = "high",
                        ["notification"] = new JsonObject { ["channel_id"] = "announcements", ["sound"] = "default" }
                    },
                    ["apns"] = new JsonObject
                    {
                        ["payload"] = new JsonObject { ["aps"] = new JsonObject { ["sound"] = "default" } },
                        ["fcm_options"] = string.IsNullOrEmpty(imageUrl) ? null : new JsonObject { ["image"] = imageUrl }
                    }
                };
                if (topics.Count == 1) message["topic"] = topics[0];
                else message["condition"] = string.Join(" || ", topics.Select(t => $"'{t}' in topics"));

                var url = _config["Firebase:SendUrl"] is { Length: > 0 } u
                    ? u : $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(new JsonObject { ["message"] = message }.ToJsonString(), Encoding.UTF8, "application/json")
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var res = await Http.SendAsync(req, ct);
                var resBody = await res.Content.ReadAsStringAsync(ct);
                if (res.IsSuccessStatusCode) return new(true, false, null);

                var code = (int)res.StatusCode;
                if (code == 401) { _accessToken = null; return new(false, true, "401 - token refreshed, trying again"); }
                return new(false, code == 429 || code >= 500, $"{code}: {Trim(resBody)}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                return new(false, true, ex.Message);
            }
            catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or InvalidOperationException or ArgumentException or NullReferenceException)
            {
                _logger.LogWarning(ex, "[Push] Firebase settings or service-account file are wrong.");
                return new(false, false, Trim(ex.Message));
            }
        }

        private static string Trim(string s) => s.Length > 450 ? s[..450] : s;
    }
}
