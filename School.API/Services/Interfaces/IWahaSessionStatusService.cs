namespace School.API.Services.Interfaces
{
    public class WahaSessionStatus
    {
        public bool Connected { get; init; }

        // Raw status string from WAHA: WORKING | STARTING | SCAN_QR_CODE |
        // STOPPED | FAILED, or ERROR/UNREACHABLE when the check itself failed,
        // or SEND_FAILING when WAHA says WORKING but has just refused a send.
        public string Status { get; init; } = "UNKNOWN";

        public string? Error { get; init; }

        public DateTime CheckedAtUtc { get; init; }
    }

    // Cached probe of the WAHA session used by TypeRoutedSmsSender (to skip
    // WAHA entirely when it's known to be down, instead of failing every
    // message first) and by the SMS page's connection badge.
    public interface IWahaSessionStatusService
    {
        /// <param name="forceRefresh">
        /// Ask WAHA again rather than use the cached answer. Also forgets a
        /// recent send failure: it is what Resume asks for after the session
        /// was restarted, and the next send is the test of whether it worked.
        /// </param>
        Task<WahaSessionStatus> GetStatusAsync(bool forceRefresh = false);

        /// <summary>
        /// WAHA reported WORKING but could not send. For a while after this the
        /// status reads SEND_FAILING (not connected), so the queue holds the
        /// rest of the batch instead of trying - and failing - each one.
        /// </summary>
        void ReportSendFailure(string reason);

        /// <summary>A send went through: whatever failure was reported is over.</summary>
        void ReportSendSuccess();
    }
}
