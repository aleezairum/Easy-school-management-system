namespace School.API.Services.Interfaces
{
    public class WahaSessionStatus
    {
        public bool Connected { get; init; }

        // Raw status string from WAHA: WORKING | STARTING | SCAN_QR_CODE |
        // STOPPED | FAILED, or ERROR/UNREACHABLE when the check itself failed.
        public string Status { get; init; } = "UNKNOWN";

        public string? Error { get; init; }

        public DateTime CheckedAtUtc { get; init; }
    }

    // Cached probe of the WAHA session used by TypeRoutedSmsSender (to skip
    // WAHA entirely when it's known to be down, instead of failing every
    // message first) and by the SMS page's connection badge.
    public interface IWahaSessionStatusService
    {
        Task<WahaSessionStatus> GetStatusAsync(bool forceRefresh = false);
    }
}
