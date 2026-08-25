using System.Net.Http.Headers;
using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // WhatsApp sender backed by the Arsal Solutions gateway (gateway.arsalsolutions.pk).
    // Text:     POST {BaseUrl}/webhooks/push-whatsapp.php  (form-urlencoded)
    //               api_key, number, message [, attachment_url]
    //           -> { "success": true, "id": <n>, "status": "..." }
    // Document: POST {BaseUrl}/webhooks/upload-file.php    (multipart/form-data)
    //               api_key, folder, pdf=<file>
    //           -> { "success": true, "attachment_url": "...", "size": <bytes> }
    //           then send the message with the returned attachment_url.
    public class ArsalSmsSender : ISmsSender
    {
        private readonly ILogger<ArsalSmsSender> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _countryCode;
        private readonly string _uploadFolder;
        private readonly string _sendPath;
        private readonly string _pushPath;
        private readonly string _uploadPath;

        public ArsalSmsSender(ILogger<ArsalSmsSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _baseUrl = (configuration["Arsal:BaseUrl"] ?? "https://gateway.arsalsolutions.pk").TrimEnd('/');
            _apiKey = configuration["Arsal:ApiKey"] ?? string.Empty;
            _countryCode = configuration["Arsal:CountryCode"] ?? "92";
            _uploadFolder = configuration["Arsal:UploadFolder"] ?? "whatsapp";

            // Real-time GET send endpoint (the "new" API used by the working
            // integration). Messages sent here deliver immediately instead of
            // sitting "pending" in the queueing push-whatsapp.php webhook.
            _sendPath = "/" + (configuration["Arsal:SendPath"] ?? "services/receive-whatsapp.php").TrimStart('/');
            // POST webhook used only when an attachment URL must be included.
            _pushPath = "/" + (configuration["Arsal:PushPath"] ?? "webhooks/push-whatsapp.php").TrimStart('/');
            _uploadPath = "/" + (configuration["Arsal:UploadPath"] ?? "webhooks/upload-file.php").TrimStart('/');

            _httpClient = new HttpClient();
        }

        // Normalize a local number to the gateway's digits-only form, e.g.
        // "0300-1234567" -> "923001234567" (country code, no leading 0, no '+').
        private string ToGatewayNumber(string phoneNumber)
        {
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
            if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return cleaned;
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            try
            {
                var number = ToGatewayNumber(phoneNumber);

                _logger.LogInformation("[Arsal] Sending to {Number}: {Message}", number, message);

                return await SendTextAsync(number, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Arsal] Error sending to {Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendFileAsync(string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            try
            {
                var number = ToGatewayNumber(phoneNumber);

                // The gateway sends documents by URL, so upload the raw bytes first to
                // obtain a hosted attachment_url, then push the message referencing it.
                var attachmentUrl = await UploadFileAsync(fileBytes, fileName, mimeType);
                if (string.IsNullOrEmpty(attachmentUrl))
                {
                    _logger.LogError("[Arsal] Upload failed for '{File}' to {Number}; document not sent.", fileName, number);
                    return false;
                }

                _logger.LogInformation("[Arsal] Sending file '{File}' ({Size} bytes) to {Number}",
                    fileName, fileBytes.Length, number);

                return await PushMessageAsync(number, caption ?? string.Empty, attachmentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Arsal] Error sending file to {Phone}", phoneNumber);
                return false;
            }
        }

        // GET {SendPath} (e.g. /services/receive-whatsapp.php?key=&number=&message=).
        // This is the real-time send API; messages deliver immediately rather than
        // being queued as "pending" by the push-whatsapp.php webhook.
        private async Task<bool> SendTextAsync(string number, string message)
        {
            var url = $"{_baseUrl}{_sendPath}"
                + "?key=" + Uri.EscapeDataString(_apiKey)
                + "&number=" + Uri.EscapeDataString(number)
                + "&message=" + Uri.EscapeDataString(message);

            var response = await _httpClient.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Arsal] Failed to send to {Number}. Status: {Status}, Response: {Response}",
                    number, response.StatusCode, responseBody);
                return false;
            }

            if (IsGatewaySuccess(responseBody))
            {
                _logger.LogInformation("[Arsal] Sent successfully to {Number}", number);
                return true;
            }

            _logger.LogError("[Arsal] Gateway reported failure for {Number}. Response: {Response}",
                number, responseBody);
            return false;
        }

        // POST {PushPath} (form-urlencoded). Used when an attachment_url must be
        // included (the GET send API is text-only). Treats the call as successful
        // only when the gateway reports success.
        private async Task<bool> PushMessageAsync(string number, string message, string? attachmentUrl)
        {
            var form = new Dictionary<string, string>
            {
                ["api_key"] = _apiKey,
                ["number"] = number,
                ["message"] = message
            };
            if (!string.IsNullOrEmpty(attachmentUrl))
                form["attachment_url"] = attachmentUrl;

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}{_pushPath}",
                new FormUrlEncodedContent(form));
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Arsal] Failed to send to {Number}. Status: {Status}, Response: {Response}",
                    number, response.StatusCode, responseBody);
                return false;
            }

            if (IsGatewaySuccess(responseBody))
            {
                _logger.LogInformation("[Arsal] Sent successfully to {Number}", number);
                return true;
            }

            _logger.LogError("[Arsal] Gateway reported failure for {Number}. Response: {Response}",
                number, responseBody);
            return false;
        }

        // POST /webhooks/upload-file.php (multipart) and return the hosted attachment_url, or null.
        private async Task<string?> UploadFileAsync(byte[] fileBytes, string fileName, string mimeType)
        {
            try
            {
                using var multipart = new MultipartFormDataContent();
                multipart.Add(new StringContent(_apiKey), "api_key");
                if (!string.IsNullOrEmpty(_uploadFolder))
                    multipart.Add(new StringContent(_uploadFolder), "folder");

                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
                multipart.Add(fileContent, "pdf", fileName);

                var response = await _httpClient.PostAsync($"{_baseUrl}{_uploadPath}", multipart);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[Arsal] File upload failed. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseBody);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True &&
                    root.TryGetProperty("attachment_url", out var url))
                {
                    var attachmentUrl = url.GetString();
                    if (!string.IsNullOrEmpty(attachmentUrl))
                    {
                        _logger.LogInformation("[Arsal] Upload verified: {Url}", attachmentUrl);
                        return attachmentUrl;
                    }
                }

                _logger.LogError("[Arsal] Upload succeeded but no attachment_url in response: {Response}", responseBody);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Arsal] Error uploading file '{File}'", fileName);
                return null;
            }
        }

        // The gateway may answer with JSON ({success:true,id:>0} / {status:"success"})
        // or plain text; accept either, mirroring the Arsal client conventions.
        private static bool IsGatewaySuccess(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True)
                        return true;
                    if (root.TryGetProperty("status", out var st) &&
                        string.Equals(st.GetString(), "success", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (root.TryGetProperty("sent", out var sent) && sent.ValueKind == JsonValueKind.True)
                        return true;
                    return false;
                }
            }
            catch (JsonException)
            {
                // Not JSON; fall back to a plain-text check.
            }

            return responseBody.ToLowerInvariant().Contains("success");
        }
    }
}
