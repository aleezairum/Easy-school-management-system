using School.API.Data.DBModels.SMS;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Routes each send by SmsMessageType: types listed in SmsRouting:WahaMessageTypes
    // (default just Absent) go out free through WAHA; everything else goes
    // through Meta's paid Cloud API. If WAHA is known to be disconnected, or a
    // WAHA send fails, the message automatically falls back to Meta so nothing
    // is silently dropped.
    public class TypeRoutedSmsSender : ISmsSender
    {
        private readonly ISmsSender _waha;
        private readonly ISmsSender _meta;
        private readonly IWahaSessionStatusService _wahaStatus;
        private readonly ILogger<TypeRoutedSmsSender> _logger;
        private readonly HashSet<SmsMessageType> _wahaTypes;
        private readonly bool _routingEnabled;

        // Reflects whichever provider actually handled the most recent send —
        // read by SmsQueueProcessor right after SendTemplateAsync returns.
        public string ProviderName { get; private set; } = "Meta";

        public TypeRoutedSmsSender(
            WahaSmsSender waha,
            MetaWhatsAppSender meta,
            IWahaSessionStatusService wahaStatus,
            ILogger<TypeRoutedSmsSender> logger,
            IConfiguration configuration)
        {
            _waha = waha;
            _meta = meta;
            _wahaStatus = wahaStatus;
            _logger = logger;

            _routingEnabled = configuration.GetValue<bool?>("SmsRouting:Enabled") ?? true;

            var configuredTypes = configuration.GetSection("SmsRouting:WahaMessageTypes").Get<string[]>()
                ?? new[] { nameof(SmsMessageType.Absent) };

            _wahaTypes = configuredTypes
                .Select(t => Enum.TryParse<SmsMessageType>(t, ignoreCase: true, out var v) ? (SmsMessageType?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToHashSet();
        }

        public async Task<SmsSendOutcome> SendTemplateAsync(string phoneNumber, SmsTemplateRequest request)
        {
            bool wantsWaha = _routingEnabled
                && request.MessageType.HasValue
                && _wahaTypes.Contains(request.MessageType.Value);

            if (wantsWaha)
            {
                var status = await _wahaStatus.GetStatusAsync();
                if (status.Connected)
                {
                    var outcome = await _waha.SendTemplateAsync(phoneNumber, request);
                    if (outcome.Success)
                    {
                        ProviderName = _waha.ProviderName;
                        return outcome;
                    }

                    _logger.LogWarning(
                        "[TypeRouted] WAHA send failed for {Phone} ({Type}); falling back to Meta. Error: {Error}",
                        phoneNumber, request.MessageType, outcome.Error);
                }
                else
                {
                    _logger.LogWarning(
                        "[TypeRouted] WAHA not connected (status={Status}); routing {Type} to Meta instead.",
                        status.Status, request.MessageType);
                }
            }

            var metaOutcome = await _meta.SendTemplateAsync(phoneNumber, request);
            ProviderName = _meta.ProviderName;
            return metaOutcome;
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            var outcome = await SendTemplateAsync(phoneNumber, new SmsTemplateRequest { RenderedText = message });
            return outcome.Success;
        }

        public async Task<bool> SendFileAsync(
            string phoneNumber, byte[] fileBytes, string fileName, string mimeType, string? caption)
        {
            var outcome = await SendTemplateAsync(phoneNumber, new SmsTemplateRequest
            {
                RenderedText = caption,
                DocumentBytes = fileBytes,
                DocumentFileName = fileName,
                DocumentMimeType = mimeType
            });
            return outcome.Success;
        }
    }
}
