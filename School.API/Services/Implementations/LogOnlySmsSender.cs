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
    }
}
