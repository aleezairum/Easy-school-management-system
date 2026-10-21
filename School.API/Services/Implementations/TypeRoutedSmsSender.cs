using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Routes each send by SmsMessageType: types listed in SmsRouting:WahaMessageTypes
    // go out free through WAHA; everything else goes through Meta's paid Cloud API.
    //
    // When a free-routed message finds WAHA disconnected it is NOT quietly
    // upgraded to the paid gateway — that would spend money the user never
    // agreed to. It comes back as a hold instead, and the queue processor parks
    // the row until the user reconnects WAHA (resume free) or explicitly
    // releases the job through the paid gateway (ForcePaid).
    public class TypeRoutedSmsSender : ISmsSender
    {
        private readonly ISmsSender _waha;
        private readonly ISmsSender _meta;
        private readonly ISmsRoutingPolicy _policy;
        private readonly IWahaSessionStatusService _wahaStatus;
        private readonly ILogger<TypeRoutedSmsSender> _logger;

        // Reflects whichever provider actually handled the most recent send —
        // read by SmsQueueProcessor right after SendTemplateAsync returns.
        public string ProviderName { get; private set; } = "Meta";

        public TypeRoutedSmsSender(
            WahaSmsSender waha,
            MetaWhatsAppSender meta,
            ISmsRoutingPolicy policy,
            IWahaSessionStatusService wahaStatus,
            ILogger<TypeRoutedSmsSender> logger)
        {
            _waha = waha;
            _meta = meta;
            _policy = policy;
            _wahaStatus = wahaStatus;
            _logger = logger;
        }

        public async Task<SmsSendOutcome> SendTemplateAsync(string phoneNumber, SmsTemplateRequest request)
        {
            var route = await _policy.DecideAsync(request.MessageType, request.ForcePaid);

            if (route == SmsRoute.Hold)
            {
                var status = await _policy.GetFreeStatusAsync();
                _logger.LogInformation(
                    "[TypeRouted] Holding {Type} for {Phone}: WAHA not connected (status={Status})",
                    request.MessageType, phoneNumber, status.Status);

                return SmsSendOutcome.Hold(
                    $"WhatsApp (free) is disconnected — status {status.Status}. Held until resumed.");
            }

            if (route == SmsRoute.Free)
            {
                var outcome = await _waha.SendTemplateAsync(phoneNumber, request);
                if (outcome.Success)
                {
                    _wahaStatus.ReportSendSuccess();
                    ProviderName = _waha.ProviderName;
                    return outcome;
                }

                // The send failed. If WAHA dropped out between the routing check
                // and now, that's a hold, not a failure — re-probe rather than
                // burning the row (or silently paying for it).
                var status = await _policy.GetFreeStatusAsync(forceRefresh: true);
                if (!status.Connected)
                {
                    _logger.LogWarning(
                        "[TypeRouted] WAHA send failed for {Phone} and the session is now {Status}; holding.",
                        phoneNumber, status.Status);

                    return SmsSendOutcome.Hold(
                        $"WhatsApp (free) went offline mid-send — status {status.Status}. Held until resumed.");
                }

                // WAHA says WORKING and still could not send - a hung WEBJS
                // session does exactly this until it is restarted. The message
                // is not at fault, so it is held, not failed; and WAHA is
                // marked as failing so the rest of the queue is held without
                // being tried one by one. Restart the session, then Resume.
                if (outcome.ErrorCode == WahaSmsSender.ServerErrorCode)
                {
                    _wahaStatus.ReportSendFailure(outcome.Error ?? "send failed");
                    _logger.LogWarning(
                        "[TypeRouted] WAHA is WORKING but refused the send to {Phone}; holding. {Error}",
                        phoneNumber, outcome.Error);

                    return SmsSendOutcome.Hold(
                        $"WhatsApp (free) shows connected but is not sending: {outcome.Error}. " +
                        "Held - restart the WAHA session, then Resume.");
                }

                // Refused for this message alone (a bad number, say): a real
                // failure, recorded with WAHA's own reason.
                _logger.LogWarning(
                    "[TypeRouted] WAHA send failed for {Phone} ({Type}) while connected. Error: {Error}",
                    phoneNumber, request.MessageType, outcome.Error);

                ProviderName = _waha.ProviderName;
                return outcome;
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
