using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // WhatsApp sender backed by Meta's official WhatsApp Business Cloud API.
    //
    // Template:  POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/messages
    //              { messaging_product, to, type:"template",
    //                template:{ name, language:{code}, components:[...] } }
    //            -> { "messages":[{ "id":"wamid.xxx" }] }
    // Document:  POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/media  (multipart)
    //              messaging_product=whatsapp, type=<mime>, file=<bytes>
    //            -> { "id":"<media_id>" }   then referenced in a header component.
    // Free text: POST .../messages with type:"text". Meta only delivers this
    //            inside the 24-hour customer service window, so school-initiated
    //            notifications must go through SendTemplateAsync.
    public class MetaWhatsAppSender : ISmsSender
    {
        // Shared instance: the queue processor resolves a new scoped sender for
        // every batch, and a per-instance HttpClient would leak sockets over the
        // life of the process. The access token is attached per request instead
        // of via DefaultRequestHeaders so a single client stays safe to share.
        private static readonly HttpClient Http = new();

        // The queue processor builds a scope (and therefore this sender) every
        // few seconds, so the misconfiguration warning is emitted only once.
        private static int _configWarningLogged;

        private readonly ILogger<MetaWhatsAppSender> _logger;
        private readonly string _messagesUrl;
        private readonly string _mediaUrl;
        private readonly string _accessToken;
        private readonly string _countryCode;

        public string ProviderName => "Meta";

        public MetaWhatsAppSender(ILogger<MetaWhatsAppSender> logger, IConfiguration configuration)
        {
            _logger = logger;

            var baseUrl       = (configuration["MetaWhatsApp:BaseUrl"] ?? "https://graph.facebook.com").TrimEnd('/');
            var apiVersion    = (configuration["MetaWhatsApp:ApiVersion"] ?? "v21.0").Trim('/');
            var phoneNumberId = (configuration["MetaWhatsApp:PhoneNumberId"] ?? string.Empty).Trim();

            _accessToken = (configuration["MetaWhatsApp:AccessToken"] ?? string.Empty).Trim();
            _countryCode = configuration["MetaWhatsApp:CountryCode"] ?? "92";

            _messagesUrl = $"{baseUrl}/{apiVersion}/{phoneNumberId}/messages";
            _mediaUrl    = $"{baseUrl}/{apiVersion}/{phoneNumberId}/media";

            if ((string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(_accessToken)) &&
                Interlocked.Exchange(ref _configWarningLogged, 1) == 0)
            {
                _logger.LogWarning(
                    "[Meta] PhoneNumberId or AccessToken is not configured — sends will fail. " +
                    "Set MetaWhatsApp:PhoneNumberId and MetaWhatsApp:AccessToken in appsettings.json.");
            }
        }

        // Normalize a local number to Meta's digits-only form, e.g.
        // "0300-1234567" -> "923001234567" (country code, no leading 0, no '+').
        private string ToWhatsAppNumber(string phoneNumber)
        {
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
            if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return cleaned;
        }

        // ─────────────────────────────────────────────────────────────
        // Template send — the path every school-initiated message takes
        // ─────────────────────────────────────────────────────────────
        public async Task<SmsSendOutcome> SendTemplateAsync(
            string phoneNumber, SmsTemplateRequest request)
        {
            try
            {
                var number = ToWhatsAppNumber(phoneNumber);

                // No template configured for this message type (blank Name in
                // MetaWhatsApp:Templates) — fall back to free-form text so the
                // send still happens and Meta's own error, if any, is recorded.
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    _logger.LogWarning(
                        "[Meta] No template configured — sending free-form text to {Number}. " +
                        "This only delivers inside the 24-hour service window.", number);

                    return await SendFreeFormAsync(number, request);
                }

                var components = new List<object>();

                // Document header (result-card PDF): upload first, then reference
                // the returned media id from the template's header component.
                if (request.HasDocument)
                {
                    var (mediaId, uploadError) = await UploadMediaAsync(
                        request.DocumentBytes!,
                        request.DocumentFileName ?? "document.pdf",
                        request.DocumentMimeType ?? "application/pdf");

                    if (string.IsNullOrEmpty(mediaId))
                        return SmsSendOutcome.Fail(
                            $"Media upload failed: {uploadError}", "media_upload_failed", uploadError);

                    components.Add(new
                    {
                        type = "header",
                        parameters = new object[]
                        {
                            new
                            {
                                type = "document",
                                document = new
                                {
                                    id = mediaId,
                                    filename = request.DocumentFileName ?? "document.pdf"
                                }
                            }
                        }
                    });
                }

                if (request.BodyParams.Count > 0)
                {
                    components.Add(new
                    {
                        type = "body",
                        parameters = request.BodyParams
                            .Select(p => (object)new { type = "text", text = SanitizeParameter(p) })
                            .ToArray()
                    });
                }

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = number,
                    type = "template",
                    template = new
                    {
                        name = request.Name,
                        language = new { code = request.Language },
                        components = components.ToArray()
                    }
                };

                _logger.LogInformation(
                    "[Meta] Sending template '{Template}' ({Lang}) to {Number} with {Count} param(s){Doc}",
                    request.Name, request.Language, number, request.BodyParams.Count,
                    request.HasDocument ? " + document" : string.Empty);

                return await PostMessageAsync(payload, number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Meta] Error sending template to {Phone}", phoneNumber);
                return SmsSendOutcome.Fail(ex.Message);
            }
        }

        // Free-form path used when a message type has no template configured.
        // Keeps the full outcome (unlike the bool-returning SendAsync) so the
        // real Meta error still lands on the row.
        private async Task<SmsSendOutcome> SendFreeFormAsync(
            string number, SmsTemplateRequest request)
        {
            if (request.HasDocument)
            {
                var (mediaId, uploadError) = await UploadMediaAsync(
                    request.DocumentBytes!,
                    request.DocumentFileName ?? "document.pdf",
                    request.DocumentMimeType ?? "application/pdf");

                if (string.IsNullOrEmpty(mediaId))
                    return SmsSendOutcome.Fail(
                        $"Media upload failed: {uploadError}", "media_upload_failed", uploadError);

                // A picture is posted as an image, so it shows in the chat
                // instead of arriving as a file to download. Meta caps either
                // caption at 1024 characters and rejects a longer one.
                if ((request.DocumentMimeType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return await PostMessageAsync(new
                    {
                        messaging_product = "whatsapp",
                        recipient_type = "individual",
                        to = number,
                        type = "image",
                        image = new
                        {
                            id = mediaId,
                            caption = Clip(request.RenderedText ?? string.Empty, 1024)
                        }
                    }, number);

                return await PostMessageAsync(new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = number,
                    type = "document",
                    document = new
                    {
                        id = mediaId,
                        filename = request.DocumentFileName ?? "document.pdf",
                        caption = request.RenderedText ?? string.Empty
                    }
                }, number);
            }

            // Tappable menu. The recipient picks a row instead of typing a
            // number, and the choice comes back as interactive.list_reply.id.
            // Meta rejects the whole message if any field is over length, so
            // everything is clipped here rather than trusted from the caller.
            if (request.HasInteractive)
            {
                var list = request.Interactive!;

                var rows = list.Rows.Take(10).Select(r => new
                {
                    id = Clip(r.Id, 200),
                    title = Clip(r.Title, 24),
                    description = Clip(r.Description ?? string.Empty, 72)
                }).ToArray();

                object interactive = string.IsNullOrWhiteSpace(list.Header)
                    ? new
                    {
                        type = "list",
                        body = new { text = Clip(list.Body, 1024) },
                        footer = new { text = Clip(list.Footer ?? string.Empty, 60) },
                        action = new
                        {
                            button = Clip(list.ButtonText, 20),
                            sections = new[] { new { title = "Options", rows } }
                        }
                    }
                    : new
                    {
                        type = "list",
                        header = new { type = "text", text = Clip(list.Header!, 60) },
                        body = new { text = Clip(list.Body, 1024) },
                        footer = new { text = Clip(list.Footer ?? string.Empty, 60) },
                        action = new
                        {
                            button = Clip(list.ButtonText, 20),
                            sections = new[] { new { title = "Options", rows } }
                        }
                    };

                return await PostMessageAsync(new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = number,
                    type = "interactive",
                    interactive
                }, number);
            }

            return await PostMessageAsync(new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = number,
                type = "text",
                text = new { preview_url = false, body = request.RenderedText ?? string.Empty }
            }, number);
        }

        private static string Clip(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        // ─────────────────────────────────────────────────────────────
        // Free-form text — only delivers inside the 24h service window
        // ─────────────────────────────────────────────────────────────
        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            try
            {
                var number = ToWhatsAppNumber(phoneNumber);

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = number,
                    type = "text",
                    text = new { preview_url = false, body = message }
                };

                _logger.LogInformation("[Meta] Sending free-form text to {Number}", number);

                var outcome = await PostMessageAsync(payload, number);
                return outcome.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Meta] Error sending text to {Phone}", phoneNumber);
                return false;
            }
        }

        // Free-form document — same 24h window restriction as SendAsync.
        public async Task<bool> SendFileAsync(
            string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            try
            {
                var number = ToWhatsAppNumber(phoneNumber);

                var (mediaId, uploadError) = await UploadMediaAsync(fileBytes, fileName, mimeType);
                if (string.IsNullOrEmpty(mediaId))
                {
                    _logger.LogError("[Meta] Upload failed for '{File}' to {Number}: {Error}",
                        fileName, number, uploadError);
                    return false;
                }

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = number,
                    type = "document",
                    document = new
                    {
                        id = mediaId,
                        filename = fileName,
                        caption = caption ?? string.Empty
                    }
                };

                _logger.LogInformation("[Meta] Sending file '{File}' ({Size} bytes) to {Number}",
                    fileName, fileBytes.Length, number);

                var outcome = await PostMessageAsync(payload, number);
                return outcome.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Meta] Error sending file to {Phone}", phoneNumber);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Graph API plumbing
        // ─────────────────────────────────────────────────────────────

        // POST the payload to /messages and translate the reply into an outcome.
        private async Task<SmsSendOutcome> PostMessageAsync(object payload, string number)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _messagesUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await Http.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var (code, message) = ParseError(body);
                _logger.LogError(
                    "[Meta] Send failed for {Number}. Status: {Status}, Code: {Code}, Error: {Error}",
                    number, response.StatusCode, code, message);
                return SmsSendOutcome.Fail(
                    message ?? $"HTTP {(int)response.StatusCode}", code, body);
            }

            var messageId = ParseMessageId(body);
            _logger.LogInformation("[Meta] Sent to {Number} (wamid {MessageId})", number, messageId);
            return SmsSendOutcome.Ok(messageId, body);
        }

        // POST /media (multipart) and return Meta's media id, or null + reason.
        private async Task<(string? mediaId, string? error)> UploadMediaAsync(
            byte[] fileBytes, string fileName, string mimeType)
        {
            try
            {
                using var multipart = new MultipartFormDataContent
                {
                    { new StringContent("whatsapp"), "messaging_product" },
                    { new StringContent(mimeType), "type" }
                };

                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                multipart.Add(fileContent, "file", fileName);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _mediaUrl)
                {
                    Content = multipart
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await Http.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var (_, message) = ParseError(body);
                    return (null, message ?? $"HTTP {(int)response.StatusCode}: {body}");
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("id", out var id))
                {
                    var mediaId = id.GetString();
                    if (!string.IsNullOrEmpty(mediaId))
                    {
                        _logger.LogInformation("[Meta] Uploaded '{File}' as media {MediaId}", fileName, mediaId);
                        return (mediaId, null);
                    }
                }

                return (null, $"Upload succeeded but no media id in response: {body}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Meta] Error uploading file '{File}'", fileName);
                return (null, ex.Message);
            }
        }

        // WhatsApp rejects template parameters that contain newlines or tabs, or
        // runs of more than four spaces (error 132000). Callers build readable
        // multi-line bodies, so collapse any whitespace run to a single space
        // here rather than relying on every caller to remember the rule.
        private static string SanitizeParameter(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            bool lastWasSpace = false;

            foreach (var ch in value)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasSpace) sb.Append(' ');
                    lastWasSpace = true;
                }
                else
                {
                    sb.Append(ch);
                    lastWasSpace = false;
                }
            }

            return sb.ToString().Trim();
        }

        // Pull "wamid.xxx" out of { "messages":[{ "id": "..." }] }.
        private static string? ParseMessageId(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                    messages.ValueKind == JsonValueKind.Array &&
                    messages.GetArrayLength() > 0 &&
                    messages[0].TryGetProperty("id", out var id))
                {
                    return id.GetString();
                }
            }
            catch (JsonException) { /* non-JSON reply — nothing to extract */ }
            return null;
        }

        // Graph errors look like
        // { "error": { "message": "...", "code": 132000,
        //              "error_data": { "details": "..." } } }
        private static (string? code, string? message) ParseError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("error", out var error))
                    return (null, null);

                string? code = error.TryGetProperty("code", out var c)
                    ? (c.ValueKind == JsonValueKind.Number ? c.GetInt64().ToString() : c.GetString())
                    : null;

                string? message = error.TryGetProperty("message", out var m) ? m.GetString() : null;

                // error_data.details carries the actionable reason (e.g. exactly
                // which template parameter count mismatched).
                if (error.TryGetProperty("error_data", out var data) &&
                    data.TryGetProperty("details", out var details))
                {
                    var detailText = details.GetString();
                    if (!string.IsNullOrWhiteSpace(detailText))
                        message = string.IsNullOrWhiteSpace(message)
                            ? detailText
                            : $"{message} — {detailText}";
                }

                return (code, message);
            }
            catch (JsonException)
            {
                return (null, null);
            }
        }
    }
}
