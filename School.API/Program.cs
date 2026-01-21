using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using School.API.Data;
using School.API.Services;
using School.API.Services.Interfaces.Academic;
using School.API.Services.Implementations.Academic;
using School.API.Repositories.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Student;
using School.API.Services.Implementations.Student;
using School.API.Repositories.Interfaces.Student;
using School.API.Repositories.Implementations.Student;
using School.API.Services.Interfaces.HR;
using School.API.Services.Implementations.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Repositories.Implementations.HR;
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key is missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAcademicSessionYearRepository, AcademicSessionYearRepository>();
builder.Services.AddScoped<ISMSSectionService, AcademicSessionYearService>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

// HR Services
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IHRGradeRepository, HRGradeRepository>();
builder.Services.AddScoped<IHRGradeService, HRGradeService>();
builder.Services.AddScoped<IAcademicGradeRepository, AcademicGradeRepository>();
builder.Services.AddScoped<IAcademicGradeService, AcademicGradeService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Swagger & CORS (optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



var app = builder.Build();

// Middleware
//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

