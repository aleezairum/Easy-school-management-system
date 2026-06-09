namespace School.API.Extensions;

public static class HttpRequestExtensions
{
    public static int GetUserId(this HttpRequest request)
    {
        return int.TryParse(request.Headers["X-User-Id"].FirstOrDefault(), out var id) ? id : 0;
    }
}
