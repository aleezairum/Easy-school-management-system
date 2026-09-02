using School.API.Data.DBModels.SMS;

namespace School.API.Services.Interfaces
{
    public enum SmsRoute
    {
        // Send now through WAHA, free.
        Free,

        // Send now through Meta's paid Cloud API.
        Paid,

        // Do not send. The row wanted the free route but WAHA is disconnected,
        // so it is parked (SmsStatus.Held) until the user reconnects WAHA and
        // resumes free, or releases the job through the paid gateway.
        Hold
    }

    // The single place that decides which gateway a message should use, shared
    // by the queue processor (which parks rows it must not send yet), the
    // routing sender, and the SMS page (which warns before a batch is queued).
    public interface ISmsRoutingPolicy
    {
        // True when this message type is configured to go out free through WAHA
        // (SmsRouting:WahaMessageTypes).
        bool PrefersFree(SmsMessageType? messageType);

        // The WAHA server's own address (Wasender:BaseUrl). The SMS page links
        // to it so the user can open the session and scan the QR.
        string FreeGatewayUrl { get; }

        Task<WahaSessionStatus> GetFreeStatusAsync(bool forceRefresh = false);

        // forcePaid comes from the row/request: the user has already chosen to
        // pay for this batch, so WAHA is skipped without a status check.
        Task<SmsRoute> DecideAsync(SmsMessageType? messageType, bool forcePaid, bool forceRefresh = false);
    }
}
