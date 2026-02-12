var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
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
=======
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

// 🔴 MUST be first
app.UsePathBase("/EasySchool");

// Static files AFTER PathBase
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
app.UseStaticFiles();

app.UseRouting();

<<<<<<< HEAD
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
=======
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68

app.Run();
