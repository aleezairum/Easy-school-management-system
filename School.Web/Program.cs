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

var app = builder.Build();

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

    if (!isLoginPage && !isSessionPing && context.Session.GetInt32("UserId") == null)
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
