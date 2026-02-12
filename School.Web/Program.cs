var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();

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


app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseAuthentication();
app.UseAuthorization();

//app.MapRazorPages();
app.MapDefaultControllerRoute();


app.Run();
