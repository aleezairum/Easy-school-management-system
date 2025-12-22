using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using School.API.Data;
using School.API.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================
// Database (DB First)
// =========================
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================
// JWT Authentication
// =========================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("JWT Key is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// =========================
// Dependency Injection
// =========================
builder.Services.AddScoped<IAuthService, AuthService>();

// =========================
// Controllers
// =========================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =========================
// Swagger
// =========================
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "School API",
//        Version = "v1"
//    });

//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Enter: Bearer {your JWT token}"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});

// =========================
// CORS
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost");
    });
});

var app = builder.Build();

// =========================
// Middleware Pipeline
// =========================
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//builder.Services.AddScoped<IAcademicSessionYearRepository, AcademicSessionYearRepository>();
//builder.Services.AddScoped<IAcademicSessionYearService, AcademicSessionYearService>();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
