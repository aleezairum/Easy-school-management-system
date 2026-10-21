using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace School.Web.Services
{
    /// <summary>
    /// The API refuses web-portal calls without a token (School.API Program.cs,
    /// Jwt:RequireForWebApi). The portal signs in through the MVC session, which
    /// lives 30 days, so instead of one long token this signs a short one for
    /// the session's user whenever the last is an hour old - on every page, the
    /// 5-minute keep-alive ping, and every call the web server itself makes.
    /// Jwt:Key must be the same as School.API's.
    /// </summary>
    public class ApiTokenService
    {
        // Must match School.API.Extensions.WebClaims.
        public const string UserIdClaim = "web_uid";
        public const string NameClaim = "web_name";

        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(4);
        private static readonly TimeSpan RenewAfter = TimeSpan.FromHours(1);
        private const string KToken = "ApiToken", KIssued = "ApiTokenIssued";

        private readonly SymmetricSecurityKey? _key;

        public ApiTokenService(IConfiguration config)
        {
            var key = config["Jwt:Key"];
            if (!string.IsNullOrWhiteSpace(key)) _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        /// <summary>The signed-in user's API token, renewed when an hour old; null when nobody is signed in.</summary>
        public string? For(HttpContext ctx)
        {
            var userId = ctx.Session.GetInt32("UserId");
            if (userId == null || _key == null) return null;

            var token = ctx.Session.GetString(KToken);
            var issuedTicks = ctx.Session.GetString(KIssued);
            if (token != null && long.TryParse(issuedTicks, out var ticks)
                && DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < RenewAfter)
                return token;

            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim(UserIdClaim, userId.Value.ToString()),
                    new Claim(NameClaim, ctx.Session.GetString("UserFullName") ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                },
                notBefore: now.AddMinutes(-1),
                expires: now.Add(Lifetime),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            token = new JwtSecurityTokenHandler().WriteToken(jwt);
            ctx.Session.SetString(KToken, token);
            ctx.Session.SetString(KIssued, now.Ticks.ToString());
            return token;
        }
    }

    /// <summary>Adds the signed-in user's token to the web server's own calls to the API (reports, certificates).</summary>
    public class ApiTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;
        private readonly ApiTokenService _tokens;

        public ApiTokenHandler(IHttpContextAccessor http, ApiTokenService tokens)
        {
            _http = http;
            _tokens = tokens;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null && _http.HttpContext is { } ctx && _tokens.For(ctx) is { } token)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
