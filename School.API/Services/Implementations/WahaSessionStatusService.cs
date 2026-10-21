using System.Text.Json;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Polls WAHA's GET /api/sessions/{session} and caches the result briefly
    // so neither the routing sender nor the UI badge hammer WAHA on every
    // message / every poll.
    public class WahaSessionStatusService : IWahaSessionStatusService
    {
        private static readonly HttpClient Http = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(20);

        private readonly ILogger<WahaSessionStatusService> _logger;
        private readonly string _baseUrl;
        private readonly string _session;
        private readonly string? _apiKey;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private WahaSessionStatus? _cached;
        private DateTime _cachedAtUtc;

        /// <summary>
        /// How long a send failure keeps WAHA counted as unavailable. When it
        /// runs out the next message is tried again; if WAHA is still stuck,
        /// that one is held too and the window starts over.
        /// </summary>
        private static readonly TimeSpan SendFailureWindow = TimeSpan.FromMinutes(10);
        private DateTime _sendFailingUntilUtc;
        private string? _sendFailure;

        public WahaSessionStatusService(ILogger<WahaSessionStatusService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _baseUrl = (configuration["Wasender:BaseUrl"] ?? configuration["Waha:BaseUrl"] ?? "http://localhost:3000")
                .TrimEnd('/');
            _session = configuration["Wasender:Session"] ?? configuration["Waha:Session"] ?? "default";
            _apiKey = configuration["Wasender:ApiKey"] ?? configuration["Waha:ApiKey"];
        }

        public void ReportSendFailure(string reason)
        {
            _sendFailure = reason;
            _sendFailingUntilUtc = DateTime.UtcNow + SendFailureWindow;
            _logger.LogWarning("[WAHA] Session reports WORKING but sending fails; holding sends for {Minutes} min. {Reason}",
                SendFailureWindow.TotalMinutes, reason);
        }

        public void ReportSendSuccess()
        {
            _sendFailingUntilUtc = default;
            _sendFailure = null;
        }

        public async Task<WahaSessionStatus> GetStatusAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
                ReportSendSuccess();
            else if (DateTime.UtcNow < _sendFailingUntilUtc)
                return new WahaSessionStatus
                {
                    Connected = false,
                    Status = "SEND_FAILING",
                    Error = _sendFailure,
                    CheckedAtUtc = DateTime.UtcNow
                };

            if (!forceRefresh && _cached != null && DateTime.UtcNow - _cachedAtUtc < CacheTtl)
                return _cached;

            await _lock.WaitAsync();
            try
            {
                if (!forceRefresh && _cached != null && DateTime.UtcNow - _cachedAtUtc < CacheTtl)
                    return _cached;

                _cached = await FetchAsync();
                _cachedAtUtc = DateTime.UtcNow;
                return _cached;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<WahaSessionStatus> FetchAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/sessions/{_session}");
                if (!string.IsNullOrEmpty(_apiKey))
                    request.Headers.Add("X-Api-Key", _apiKey);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await Http.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[WAHA] Session status check failed: HTTP {Status}", response.StatusCode);
                    return new WahaSessionStatus
                    {
                        Connected = false,
                        Status = "ERROR",
                        Error = $"HTTP {(int)response.StatusCode}",
                        CheckedAtUtc = DateTime.UtcNow
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

                return new WahaSessionStatus
                {
                    Connected = string.Equals(status, "WORKING", StringComparison.OrdinalIgnoreCase),
                    Status = status ?? "UNKNOWN",
                    CheckedAtUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WAHA] Session status check failed");
                return new WahaSessionStatus
                {
                    Connected = false,
                    Status = "UNREACHABLE",
                    Error = ex.Message,
                    CheckedAtUtc = DateTime.UtcNow
                };
            }
        }
    }
}
