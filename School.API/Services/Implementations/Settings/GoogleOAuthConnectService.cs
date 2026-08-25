using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using School.API.Services.Interfaces.Settings;

namespace School.API.Services.Implementations.Settings
{
    public class GoogleOAuthConnectService : IGoogleOAuthConnectService
    {
        private readonly ILogger<GoogleOAuthConnectService> _logger;

        public GoogleOAuthConnectService(ILogger<GoogleOAuthConnectService> logger)
        {
            _logger = logger;
        }

        public string BuildAuthorizationUrl(string clientId, string clientSecret, string redirectUri, string state)
        {
            var flow = BuildFlow(clientId, clientSecret);
            var request = (GoogleAuthorizationCodeRequestUrl)flow.CreateAuthorizationCodeRequest(redirectUri);
            // access_type=offline is required for Google to issue a refresh token at all.
            // prompt=consent forces the consent screen every time so Google reliably issues
            // one even on a re-connect (by default it's only issued on the user's very
            // first consent, which would silently break subsequent reconnects).
            request.AccessType = "offline";
            request.Prompt = "consent";
            request.State = state;
            return request.Build().ToString();
        }

        public async Task<OAuthExchangeResult> ExchangeCodeForRefreshTokenAsync(
            string clientId, string clientSecret, string redirectUri, string code, CancellationToken ct)
        {
            try
            {
                var flow = BuildFlow(clientId, clientSecret);
                var token = await flow.ExchangeCodeForTokenAsync("db-backup-job", code, redirectUri, ct);

                if (string.IsNullOrWhiteSpace(token.RefreshToken))
                {
                    // The request always carries access_type=offline and
                    // prompt=consent, so Google withholding the refresh token
                    // here is genuinely unusual — worth its own message.
                    _logger.LogWarning("[GoogleOAuthConnectService] Exchange succeeded but no refresh token was issued");
                    return new OAuthExchangeResult(null,
                        "Google accepted the sign-in but issued no refresh token. Revoke this app at " +
                        "myaccount.google.com/permissions and click Connect again.");
                }

                return new OAuthExchangeResult(token.RefreshToken, null);
            }
            catch (TokenResponseException ex)
            {
                // Google's own reason: invalid_client (wrong Client ID/Secret),
                // redirect_uri_mismatch, invalid_grant (code reused/expired)…
                var detail = string.IsNullOrWhiteSpace(ex.Error?.ErrorDescription)
                    ? ex.Error?.Error
                    : $"{ex.Error?.Error} — {ex.Error?.ErrorDescription}";
                _logger.LogError(ex, "[GoogleOAuthConnectService] Code exchange rejected by Google: {Detail}", detail);
                return new OAuthExchangeResult(null, $"Google rejected the request: {detail}");
            }
            catch (Exception ex)
            {
                // Usually the server not being able to reach oauth2.googleapis.com
                // (firewall/proxy) rather than anything about the OAuth setup.
                _logger.LogError(ex, "[GoogleOAuthConnectService] Code exchange failed");
                return new OAuthExchangeResult(null, $"Could not reach Google to complete sign-in: {ex.Message}");
            }
        }

        private static GoogleAuthorizationCodeFlow BuildFlow(string clientId, string clientSecret) =>
            new(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                Scopes = new[] { DriveService.Scope.DriveFile }
            });
    }
}
