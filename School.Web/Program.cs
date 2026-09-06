using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
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

app.UseHttpsRedirection();

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

    if (!isLoginPage && !isSessionPing && !isPublicPolicyPage && context.Session.GetInt32("UserId") == null)
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
