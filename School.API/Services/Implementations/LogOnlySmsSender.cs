using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class LogOnlySmsSender : ISmsSender
    {
        private readonly ILogger<LogOnlySmsSender> _logger;

        public LogOnlySmsSender(ILogger<LogOnlySmsSender> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("[SMS-LOG] To: {Phone} | Message: {Message}", phoneNumber, message);
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            _logger.LogInformation(
                "[SMS-LOG] To: {Phone} | File: {File} ({Size} bytes, {Mime}) | Caption: {Caption}",
                phoneNumber, fileName, fileBytes?.Length ?? 0, mimeType, caption);
            return Task.FromResult(true);
        }
    }
}
