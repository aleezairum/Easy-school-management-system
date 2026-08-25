using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // WhatsApp sender backed by WasenderAPI (https://wasenderapi.com).
    // Text:     POST {BaseUrl}/api/send-message  { to, text }
    // Document: upload raw bytes to {BaseUrl}/api/upload to get a public URL,
    //           then POST {BaseUrl}/api/send-message { to, documentUrl, fileName, text }.
    // Auth is a Bearer token (the WhatsApp session API key).
    public class WasenderSmsSender : ISmsSender
    {
        private readonly ILogger<WasenderSmsSender> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _countryCode;

        public WasenderSmsSender(ILogger<WasenderSmsSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _baseUrl = (configuration["Wasender:BaseUrl"] ?? "https://www.wasenderapi.com").TrimEnd('/');
            _countryCode = configuration["Wasender:CountryCode"] ?? "92";

            _httpClient = new HttpClient();

            var apiKey = configuration["Wasender:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        // Normalize a local number to E.164 form, e.g. "0300-1234567" -> "+923001234567".
        private string ToE164(string phoneNumber)
        {
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
            if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return "+" + cleaned;
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            try
            {
                var to = ToE164(phoneNumber);

                var payload = new
                {
                    to = to,
                    text = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[Wasender] Sending to {To}: {Message}", to, message);

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/send-message", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[Wasender] Sent successfully to {To}", to);
                    return true;
                }

                _logger.LogError("[Wasender] Failed to send to {To}. Status: {Status}, Response: {Response}",
                    to, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Wasender] Error sending to {Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendFileAsync(string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            try
            {
                var to = ToE164(phoneNumber);

                // WasenderAPI's send-message accepts only a publicly-accessible documentUrl,
                // so first upload the raw bytes to obtain a (24h) public URL.
                var documentUrl = await UploadMediaAsync(fileBytes, fileName, mimeType);
                if (string.IsNullOrEmpty(documentUrl))
                {
                    _logger.LogError("[Wasender] Upload failed for '{File}' to {To}; document not sent.", fileName, to);
                    return false;
                }

                var payload = new
                {
                    to = to,
                    documentUrl = documentUrl,
                    fileName = fileName,
                    text = caption ?? string.Empty
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[Wasender] Sending file '{File}' ({Size} bytes) to {To}",
                    fileName, fileBytes.Length, to);

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/send-message", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[Wasender] File sent successfully to {To}", to);
                    return true;
                }

                _logger.LogError("[Wasender] Failed to send file to {To}. Status: {Status}, Response: {Response}",
                    to, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Wasender] Error sending file to {Phone}", phoneNumber);
                return false;
            }
        }

        // Uploads raw bytes as a base64 data URI to /api/upload and returns the public URL,
        // or null on failure.
        private async Task<string?> UploadMediaAsync(byte[] fileBytes, string fileName, string mimeType)
        {
            try
            {
                var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(fileBytes)}";
                var payload = new
                {
                    base64 = dataUri,
                    fileName = fileName
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/upload", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[Wasender] Media upload failed. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseBody);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Response shape: { "success": true, "data": { "publicUrl": "..." } } or { "publicUrl": "..." }
                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("publicUrl", out var nestedUrl))
                {
                    return nestedUrl.GetString();
                }
                if (root.TryGetProperty("publicUrl", out var url))
                {
                    return url.GetString();
                }

                _logger.LogError("[Wasender] Media upload succeeded but no publicUrl in response: {Response}", responseBody);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Wasender] Error uploading media '{File}'", fileName);
                return null;
            }
        }
    }
}
