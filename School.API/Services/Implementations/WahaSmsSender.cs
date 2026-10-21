using System.Text;
using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class WahaSmsSender : ISmsSender
    {
        /// <summary>
        /// ErrorCode of a send WAHA could not carry out for a reason on ITS side -
        /// a 5xx, a timeout, no answer. WAHA Core's WEBJS engine is known to report
        /// the session WORKING while every send fails (after a WhatsApp Web update,
        /// or when its browser hangs) until the session is restarted. Such a
        /// message is not at fault and is held, not failed - see TypeRoutedSmsSender.
        /// </summary>
        public const string ServerErrorCode = "WAHA_SERVER";

        /// <summary>ErrorCode of a send WAHA refused for this message (a 4xx): a bad number, say.</summary>
        public const string RejectedErrorCode = "WAHA_REJECTED";

        private readonly ILogger<WahaSmsSender> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _session;
        private readonly string _countryCode;

        public WahaSmsSender(ILogger<WahaSmsSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            // The live WAHA settings currently sit under the "Wasender" section
            // (historical name); fall back to a dedicated "Waha" section if present.
            // Trim any trailing slash so building "{_baseUrl}/api/..." can never
            // produce a double slash ("//api/..."), which WAHA rejects with 401.
            _baseUrl = (configuration["Wasender:BaseUrl"] ?? configuration["Waha:BaseUrl"] ?? "http://localhost:3000")
                .TrimEnd('/');
            _session = configuration["Wasender:Session"] ?? configuration["Waha:Session"] ?? "default";
            _countryCode = configuration["Wasender:CountryCode"] ?? configuration["Waha:CountryCode"] ?? "92";

            // A hung WEBJS session can leave a request open for minutes; the
            // default 100 s would stall the whole queue on every message.
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

            var apiKey = configuration["Wasender:ApiKey"] ?? configuration["Waha:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            }
        }

        // Normalize a local number to WhatsApp chatId form "<country><number>@c.us".
        private string ToChatId(string phoneNumber)
        {
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
            if (cleaned.StartsWith("00"))          // 0092... international prefix
                cleaned = cleaned.Substring(2);
            else if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return $"{cleaned}@c.us";
        }

        /// <summary>
        /// Sends text or a file and says exactly what happened. Before this the
        /// queue only ever recorded "Sender returned false": WAHA's own answer
        /// went to the console log, which is not kept, so a failure could not
        /// be explained afterwards. It is now on the message row.
        /// </summary>
        public async Task<SmsSendOutcome> SendTemplateAsync(string phoneNumber, SmsTemplateRequest request)
        {
            var text = request.RenderedText ?? string.Empty;
            var chatId = ToChatId(phoneNumber);

            if (request.HasDocument)
            {
                var mime = request.DocumentMimeType ?? "application/pdf";
                // A picture through sendImage shows in the chat; sendFile would
                // deliver it as a document. Same body for both. (Both are WAHA
                // Plus features: WAHA Core answers them with a 4xx.)
                var endpoint = mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "sendImage" : "sendFile";
                return await PostAsync(endpoint, chatId, new
                {
                    session = _session,
                    chatId,
                    file = new
                    {
                        mimetype = mime,
                        filename = request.DocumentFileName ?? "document.pdf",
                        data = Convert.ToBase64String(request.DocumentBytes!)
                    },
                    caption = text
                });
            }

            return await PostAsync("sendText", chatId, new { session = _session, chatId, text });
        }

        private async Task<SmsSendOutcome> PostAsync(string endpoint, string chatId, object payload)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                _logger.LogInformation("[WAHA] {Endpoint} to {ChatId}", endpoint, chatId);

                using var response = await _httpClient.PostAsync($"{_baseUrl}/api/{endpoint}", content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return SmsSendOutcome.Ok(MessageId(body), Clip(body, 1000));

                var code = (int)response.StatusCode;
                // 5xx, 408 and 429 are WAHA's trouble, not the message's.
                var serverSide = code >= 500 || code == 408 || code == 429;
                _logger.LogError("[WAHA] {Endpoint} to {ChatId} failed: HTTP {Status} {Body}", endpoint, chatId, code, body);

                return SmsSendOutcome.Fail(
                    $"WAHA {endpoint} HTTP {code}: {Clip(Reason(body), 300)}",
                    serverSide ? ServerErrorCode : RejectedErrorCode,
                    Clip(body, 4000));
            }
            catch (Exception ex)
            {
                // Timeout, refused connection, DNS - WAHA did not answer.
                _logger.LogError(ex, "[WAHA] {Endpoint} to {ChatId} failed", endpoint, chatId);
                return SmsSendOutcome.Fail($"WAHA {endpoint} did not answer: {ex.Message}", ServerErrorCode);
            }
        }

        // WAHA answers a send with the message object; its id is "id" (a string,
        // or an object with "_serialized" on the WEBJS engine).
        private static string? MessageId(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("id", out var id)) return null;
                if (id.ValueKind == JsonValueKind.String) return id.GetString();
                if (id.ValueKind == JsonValueKind.Object && id.TryGetProperty("_serialized", out var s)) return s.GetString();
            }
            catch (JsonException) { }
            return null;
        }

        // The readable part of a WAHA error: its "message" (or "exception.message"), else the raw body.
        private static string Reason(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) return m.GetString()!;
                if (root.TryGetProperty("exception", out var e) && e.TryGetProperty("message", out var em)) return em.GetString() ?? body;
            }
            catch (JsonException) { }
            return string.IsNullOrWhiteSpace(body) ? "(no response body)" : body;
        }

        private static string Clip(string? s, int max) =>
            string.IsNullOrEmpty(s) ? string.Empty : s.Length <= max ? s : s[..max];

        public async Task<bool> SendFileAsync(string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption) =>
            (await SendTemplateAsync(phoneNumber, new SmsTemplateRequest
            {
                RenderedText = caption,
                DocumentBytes = fileBytes,
                DocumentFileName = fileName,
                DocumentMimeType = mimeType
            })).Success;

        public async Task<bool> SendAsync(string phoneNumber, string message) =>
            (await SendTemplateAsync(phoneNumber, new SmsTemplateRequest { RenderedText = message })).Success;
    }
}
