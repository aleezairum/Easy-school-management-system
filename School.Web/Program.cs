using Microsoft.AspNetCore.HttpOverrides;

// Same reason as in School.API: the certificates and the result card are
// rendered here, and "MMM" is spelled by the current culture.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture =
    System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
// The API wants a token on every call (Services/ApiToken.cs): pages get it
// from the layout / keep-alive ping, and the web server's own HttpClient
// calls get it from this handler.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<School.Web.Services.ApiTokenService>();
builder.Services.AddTransient<School.Web.Services.ApiTokenHandler>();
builder.Services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler<School.Web.Services.ApiTokenHandler>());
// The sign-in lasts up to 30 days of no use, but only while the browser stays open:
// the cookie has no MaxAge, so closing the browser or restarting the PC signs out.
// (Chrome's "Continue where you left off" setting keeps such cookies across restarts.)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Forwarded headers — TLS terminates at Cloudflare and again at IIS, so Kestrel
// only ever sees plain HTTP. Without this the app believes every request is
// insecure: UseHttpsRedirection below would bounce it to https, Cloudflare
// would fetch http again, and the two would loop until the browser gives up
// with ERR_TOO_MANY_REDIRECTS. Reading X-Forwarded-Proto tells it the truth.
// (School.API already does this — same settings, kept in step.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // Trust the upstream proxy — the defaults only trust loopback, which is
    // not where Cloudflare or IIS sit. Safe only because the origin firewall
    // limits port 443 to Cloudflare's ranges and the LAN.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Must run before anything that inspects the scheme or the client's address.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Only requests that arrive on a HOSTNAME get bounced to https.
//
// The school's own machines reach the app by LAN address —
// http://192.168.100.30:5000/EasySchool — and an IP can never carry a valid
// certificate: Let's Encrypt does not issue for private addresses, and the
// wildcard covers names, not numbers. Redirecting those to https therefore
// lands the user on "Your connection is not private" instead of the login
// screen, which reads as the software being down.
//
// localhost is exempt for the same reason, so the app can be run and tested on
// the server itself without a certificate.
app.UseWhen(
    ctx => !System.Net.IPAddress.TryParse(ctx.Request.Host.Host, out _)
           && !string.Equals(ctx.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseHttpsRedirection());

// ✅ REQUIRED in .NET 8 for static files (wwwroot)

//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();
//builder.Services.AddHttpClient();



// 🔴 MUST be first
app.UsePathBase("/EasySchool");

// Static files AFTER PathBase

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Redirect to login if session expired (except login/logout pages and static files)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimStart('/').ToLower() ?? "";
    var isLoginPage = path == "" || path == "login" || path.StartsWith("lib/") || path.StartsWith("js/") || path.StartsWith("css/");
    var isSessionPing = path == "session/ping";

    // Registered with Meta as the app's Privacy Policy and Data Deletion URLs,
    // so they have to be readable by anyone — Meta's reviewer included. Without
    // this they would be bounced to the login page and app review would fail.
    var isPublicPolicyPage = path == "privacy" || path == "data-deletion";

    // The mobile app download page: parents open it from the QR code on their
    // login slip, before they have any sign-in here.
    var isAppDownload = path == "app" || path == "app/download" || path == "app/qr.png";

    if (!isLoginPage && !isSessionPing && !isPublicPolicyPage && !isAppDownload && context.Session.GetInt32("UserId") == null)
    {
        context.Response.Redirect("/EasySchool/login");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseAuthentication();
app.UseAuthorization();

//app.MapRazorPages();
app.MapDefaultControllerRoute();


app.Run();
