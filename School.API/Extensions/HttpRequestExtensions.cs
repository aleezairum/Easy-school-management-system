namespace School.API.Extensions;

/// <summary>
/// The office user behind a web-portal request. School.Web signs a short JWT
/// for the signed-in user (claim <see cref="WebClaims.UserId"/>, same Jwt:Key)
/// and sends it on every API call; the API refuses calls without one
/// (Program.cs, fallback policy - Jwt:RequireForWebApi).
/// </summary>
public static class WebClaims
{
    public const string UserId = "web_uid";
    public const string Name = "web_name";
}

public static class HttpRequestExtensions
{
    public static int GetUserId(this HttpRequest request)
    {
        // The signed token is the truth; the X-User-Id header is only read
        // while Jwt:RequireForWebApi is switched off.
        if (int.TryParse(request.HttpContext.User.FindFirst(WebClaims.UserId)?.Value, out var uid)) return uid;
        return int.TryParse(request.Headers["X-User-Id"].FirstOrDefault(), out var id) ? id : 0;
    }
}
