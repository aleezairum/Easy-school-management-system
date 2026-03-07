using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using School.API.Data;
using School.API.Services;
using School.API.Services.Interfaces.Academic;
using School.API.Services.Implementations.Academic;
using School.API.Repositories.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;


using School.API.Services.Interfaces.Academic;
using School.API.Services.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Repositories.Implementations.Academic;
using School.API.Services.Interfaces.HR;
using School.API.Services.Implementations.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Repositories.Implementations.HR;
using School.API.Services.Interfaces.Accounts;
using School.API.Services.Implementations.Accounts;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Repositories.Implementations.Accounts;
using School.API.Services.Interfaces;
using School.API.Services.Implementations;
using School.API.Repositories.Interfaces;
using School.API.Repositories.Implementations;
using School.API.BackgroundJobs;
using School.API.Repositories.Implementations.Student;
using School.API.Repositories.Interfaces.Student;
using School.API.Services.Implementations.Student;
using School.API.Services.Interfaces.Student;

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


builder.Services.AddScoped<IStdAttendenceRepository, StdAttendenceRepository>();
builder.Services.AddScoped<IStdAttendenceService, StdAttendenceService>();


// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAcademicSessionYearRepository, AcademicSessionYearRepository>();
builder.Services.AddScoped<IAcademicSessionYearService, AcademicSessionYearService>();


builder.Services.AddScoped<ISMSSectionRepository, SMSSectionRepository>();
builder.Services.AddScoped<ISMSSectionService, SMSSectionService>();
builder.Services.AddScoped<ISMSClassRepository, SMSClassRepository>();
builder.Services.AddScoped<ISMSClassService, SMSClassService>();
builder.Services.AddScoped<ISMSCampusRepository, SMSCampusRepository>();
builder.Services.AddScoped<ISMSCampusService, SMSCampusService>();
builder.Services.AddScoped<IStudentStatusRepository, StudentStatusRepository>();
builder.Services.AddScoped<IStudentStatusService, StudentStatusService>();
builder.Services.AddScoped<School.API.Repositories.Interfaces.Academic.IStudentRepository, School.API.Repositories.Implementations.Academic.StudentRepository>();
builder.Services.AddScoped<School.API.Services.Interfaces.Academic.IStudentService, School.API.Services.Implementations.Academic.StudentService>();

// HR Services
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IHRGradeRepository, HRGradeRepository>();
builder.Services.AddScoped<IHRGradeService, HRGradeService>();
builder.Services.AddScoped<IAcademicGradeRepository, AcademicGradeRepository>();
builder.Services.AddScoped<IAcademicGradeService, AcademicGradeService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
builder.Services.AddScoped<ISalaryService, SalaryService>();

// Academic Services (new)
builder.Services.AddScoped<ITimeTableRepository, TimeTableRepository>();
builder.Services.AddScoped<ITimeTableService, TimeTableService>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IExamService, ExamService>();

// Accounts Services
builder.Services.AddScoped<IFeeTypeRepository, FeeTypeRepository>();
builder.Services.AddScoped<ISMSFeeTypeService, FeeTypeService>();
builder.Services.AddScoped<School.API.Repositories.Interfaces.Accounts.IFeeStructureRepository, School.API.Repositories.Implementations.Accounts.FeeStructureRepository>();
builder.Services.AddScoped<School.API.Services.Interfaces.Accounts.IFeeStructureService, School.API.Services.Implementations.Accounts.FeeStructureService>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
//builder.Services.AddScoped<IChallanVoucherRepository, ChallanVoucherRepository>();
//builder.Services.AddScoped<IChallanVoucherService, ChallanVoucherService>();
//builder.Services.AddScoped<IFeeVoucherRepository, FeeVoucherRepository>();
//builder.Services.AddScoped<IFeeVoucherService, FeeVoucherService>();

// SMS Services
builder.Services.AddScoped<ISmsMessageRepository, SmsMessageRepository>();
// Swap sender: LogOnlySmsSender (logs only) or WahaSmsSender (WhatsApp via WAHA)
// builder.Services.AddScoped<ISmsSender, LogOnlySmsSender>();
builder.Services.AddScoped<ISmsSender, WahaSmsSender>();
builder.Services.AddScoped<ISmsService, SmsService>();

// Background Jobs
builder.Services.AddHostedService<AbsentNotificationJob>();
builder.Services.AddHostedService<FeeReminderJob>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Swagger & CORS (optional)
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 
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


//app.UsePathBase("/EasySchool"); 
app.UseSwagger();           
app.UseSwaggerUI();        
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

