namespace School.API.Services.Interfaces.Settings
{
    // Outcome of trading the authorization code for a refresh token.  The two
    // failure modes are kept apart on purpose: Google rejecting the exchange
    // (bad client id/secret, redirect mismatch, no internet…) needs a very
    // different fix from Google accepting it but withholding a refresh token,
    // and a single "did not return a refresh token" message hid that difference.
    public record OAuthExchangeResult(string? RefreshToken, string? Error)
    {
        public bool Success => !string.IsNullOrWhiteSpace(RefreshToken);
    }

    // Drives the in-app "Connect with Google" flow for the DB Backup job's
    // Google Drive upload (OAuth mode) — builds the consent redirect URL and
    // exchanges the returned authorization code for a refresh token.
    public interface IGoogleOAuthConnectService
    {
        string BuildAuthorizationUrl(string clientId, string clientSecret, string redirectUri, string state);

        Task<OAuthExchangeResult> ExchangeCodeForRefreshTokenAsync(
            string clientId, string clientSecret, string redirectUri, string code, CancellationToken ct);
    }
}
