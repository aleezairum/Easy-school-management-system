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
            _baseUrl = configuration["Waha:BaseUrl"] ?? "http://localhost:3000";
            _session = configuration["Waha:Session"] ?? "default";
            _countryCode = configuration["Waha:CountryCode"] ?? "92";

            _httpClient = new HttpClient();

            var apiKey = configuration["Waha:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            }
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            try
            {
                // Clean phone number: remove spaces, dashes, leading 0
                var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
                if (cleaned.StartsWith("0"))
                    cleaned = cleaned.Substring(1);

                // Add country code if not already present
                if (!cleaned.StartsWith(_countryCode))
                    cleaned = _countryCode + cleaned;

                var chatId = $"{cleaned}@c.us";

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
