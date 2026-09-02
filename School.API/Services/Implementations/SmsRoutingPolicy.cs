using School.API.Data.DBModels.SMS;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Reads SmsRouting:* once and answers "free, paid, or hold?" for any
    // message. Registered as a singleton because the answer depends only on
    // configuration plus the (already cached) WAHA session probe.
    public class SmsRoutingPolicy : ISmsRoutingPolicy
    {
        private readonly IWahaSessionStatusService _wahaStatus;
        private readonly HashSet<SmsMessageType> _freeTypes;
        private readonly bool _routingEnabled;

        public string FreeGatewayUrl { get; }

        public SmsRoutingPolicy(IWahaSessionStatusService wahaStatus, IConfiguration configuration)
        {
            _wahaStatus = wahaStatus;
            _routingEnabled = configuration.GetValue<bool?>("SmsRouting:Enabled") ?? true;

            FreeGatewayUrl =
                (configuration["Wasender:BaseUrl"] ?? configuration["Waha:BaseUrl"] ?? string.Empty)
                .TrimEnd('/');

            var configuredTypes = configuration.GetSection("SmsRouting:WahaMessageTypes").Get<string[]>()
                ?? new[] { nameof(SmsMessageType.Absent) };

            _freeTypes = configuredTypes
                .Select(t => Enum.TryParse<SmsMessageType>(t, ignoreCase: true, out var v) ? (SmsMessageType?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToHashSet();
        }

        public bool PrefersFree(SmsMessageType? messageType)
            => _routingEnabled && messageType.HasValue && _freeTypes.Contains(messageType.Value);

        public Task<WahaSessionStatus> GetFreeStatusAsync(bool forceRefresh = false)
            => _wahaStatus.GetStatusAsync(forceRefresh);

        public async Task<SmsRoute> DecideAsync(
            SmsMessageType? messageType, bool forcePaid, bool forceRefresh = false)
        {
            if (forcePaid || !PrefersFree(messageType))
                return SmsRoute.Paid;

            var status = await GetFreeStatusAsync(forceRefresh);
            return status.Connected ? SmsRoute.Free : SmsRoute.Hold;
        }
    }
}
