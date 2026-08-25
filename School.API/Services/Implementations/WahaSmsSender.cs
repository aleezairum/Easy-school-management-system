using System.Text;
using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class WahaSmsSender : ISmsSender
    {
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

            _httpClient = new HttpClient();

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
            if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return $"{cleaned}@c.us";
        }

        public async Task<bool> SendFileAsync(string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            try
            {
                var chatId = ToChatId(phoneNumber);

                // WAHA /api/sendFile accepts the file inline as base64 data.
                var payload = new
                {
                    session = _session,
                    chatId = chatId,
                    file = new
                    {
                        mimetype = mimeType,
                        filename = fileName,
                        data = Convert.ToBase64String(fileBytes)
                    },
                    caption = caption ?? string.Empty
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[WAHA] Sending file '{File}' ({Size} bytes) to {ChatId}",
                    fileName, fileBytes.Length, chatId);

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/sendFile", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WAHA] File sent successfully to {ChatId}", chatId);
                    return true;
                }

                _logger.LogError("[WAHA] Failed to send file to {ChatId}. Status: {Status}, Response: {Response}",
                    chatId, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WAHA] Error sending file to {Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            try
            {
                var chatId = ToChatId(phoneNumber);

                var payload = new
                {
                    session = _session,
                    chatId = chatId,
                    text = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[WAHA] Sending to {ChatId}: {Message}", chatId, message);

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/sendText", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WAHA] Sent successfully to {ChatId}", chatId);
                    return true;
                }
                else
                {
                    _logger.LogError("[WAHA] Failed to send to {ChatId}. Status: {Status}, Response: {Response}",
                        chatId, response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WAHA] Error sending to {Phone}", phoneNumber);
                return false;
            }
        }
    }
}
