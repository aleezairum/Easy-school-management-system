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
using School.API.Repositories.Implementations.UserManagement;
using School.API.Repositories.Interfaces.UserManagement;
using School.API.Services.Implementations.UserManagement;
using School.API.Services.Interfaces.UserManagement;
using School.API.Repositories.Implementations.Settings;
using School.API.Repositories.Interfaces.Settings;
using School.API.Services.Implementations.Settings;
using School.API.Services.Interfaces.Settings;

var builder = WebApplication.CreateBuilder(args);

// Database — EnableRetryOnFailure added per HR-Payroll-Final-Plan.md section 9
// (transient connection blips shouldn't fail a payroll run or attendance save).
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure().UseCompatibilityLevel(120)));

// Used to hold short-lived OAuth "state" nonces for the Google Drive connect flow,
// and (HR Payroll) to cache shifts/holidays/pay components — section 9.
builder.Services.AddMemoryCache();

// Response compression — the HR attendance grid and salary sheet are the
// largest JSON payloads in the app; compresses ~80% (section 9).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

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

// User Management
builder.Services.AddScoped<ISecUserRepository, SecUserRepository>();
builder.Services.AddScoped<ISecUserService, SecUserService>();

// Fee
builder.Services.AddScoped<IFeeVoucherRepository, FeeVoucherRepository>();
builder.Services.AddScoped<IFeeVoucherService, FeeVoucherService>();
builder.Services.AddScoped<IApplyFeeRepository, ApplyFeeRepository>();
builder.Services.AddScoped<IApplyFeeService, ApplyFeeService>();

// Student
builder.Services.AddScoped<IStdAttendenceRepository, StdAttendenceRepository>();
builder.Services.AddScoped<IStdAttendenceService, StdAttendenceService>();
builder.Services.AddScoped<IStudentStatusRepository, StudentStatusRepository>();
builder.Services.AddScoped<IStudentStatusService, StudentStatusService>();
builder.Services.AddScoped<School.API.Repositories.Interfaces.Academic.IStudentRepository,
                           School.API.Repositories.Implementations.Academic.StudentRepository>();
builder.Services.AddScoped<School.API.Services.Interfaces.Academic.IStudentService,
                           School.API.Services.Implementations.Academic.StudentService>();

// Academic
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAcademicSessionYearRepository, AcademicSessionYearRepository>();
builder.Services.AddScoped<IAcademicSessionYearService, AcademicSessionYearService>();
builder.Services.AddScoped<ISMSSectionRepository, SMSSectionRepository>();
builder.Services.AddScoped<ISMSSectionService, SMSSectionService>();
builder.Services.AddScoped<ISMSSubjectRepository, SMSSubjectRepository>();
builder.Services.AddScoped<ISMSSubjectService, SMSSubjectService>();
builder.Services.AddScoped<ISMSExamRepository, SMSExamRepository>();
builder.Services.AddScoped<ISMSExamService, SMSExamService>();
builder.Services.AddScoped<ISMSExamResultRepository, SMSExamResultRepository>();
builder.Services.AddScoped<ISMSExamResultService, SMSExamResultService>();
builder.Services.AddScoped<IStaffSubjectRepository, StaffSubjectRepository>();
builder.Services.AddScoped<IStaffSubjectService, StaffSubjectService>();
builder.Services.AddScoped<ISMSClassRepository, SMSClassRepository>();
builder.Services.AddScoped<ISMSClassService, SMSClassService>();
builder.Services.AddScoped<ISMSCampusRepository, SMSCampusRepository>();
builder.Services.AddScoped<ISMSCampusService, SMSCampusService>();
builder.Services.AddScoped<ITimeTableRepository, TimeTableRepository>();
builder.Services.AddScoped<ITimeTableService, TimeTableService>();
builder.Services.AddScoped<IAcademicGradeRepository, AcademicGradeRepository>();
builder.Services.AddScoped<IAcademicGradeService, AcademicGradeService>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<ICertificateService, CertificateService>();

// HR
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IHRGradeRepository, HRGradeRepository>();
builder.Services.AddScoped<IHRGradeService, HRGradeService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// HR Payroll (HR-Payroll-Final-Plan.md)
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<ISchoolTimingRepository, SchoolTimingRepository>();
builder.Services.AddScoped<ISchoolTimingService, SchoolTimingService>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayComponentRepository, PayComponentRepository>();
builder.Services.AddScoped<IPayComponentService, PayComponentService>();
builder.Services.AddScoped<IEmployeePayRepository, EmployeePayRepository>();
builder.Services.AddScoped<IEmployeePayService, EmployeePayService>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IHrReportsRepository, HrReportsRepository>();

// Accounts
builder.Services.AddScoped<IFeeTypeRepository, FeeTypeRepository>();
builder.Services.AddScoped<ISMSFeeTypeService, FeeTypeService>();
builder.Services.AddScoped<School.API.Repositories.Interfaces.Accounts.IFeeStructureRepository,
                           School.API.Repositories.Implementations.Accounts.FeeStructureRepository>();
builder.Services.AddScoped<School.API.Services.Interfaces.Accounts.IFeeStructureService,
                           School.API.Services.Implementations.Accounts.FeeStructureService>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();

// Account Ledger reports — Student Ledger + Family Ledger (Scripts/SpRPT_Ledger.sql)
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<ILedgerService, LedgerService>();

// SMS
builder.Services.AddScoped<ISmsMessageRepository, SmsMessageRepository>();
// Active sender: TypeRoutedSmsSender sends the types listed in
// SmsRouting:WahaMessageTypes free through WAHA and everything else through
// Meta's paid Cloud API. When WAHA is disconnected a free-routed message is
// HELD rather than silently upgraded to the paid gateway — the user resumes it
// free once WAHA is back, or releases it as paid from the SMS page.
// Both concrete senders are registered so the router can use either.
builder.Services.AddScoped<WahaSmsSender>();
builder.Services.AddScoped<MetaWhatsAppSender>();
builder.Services.AddSingleton<IWahaSessionStatusService, WahaSessionStatusService>();
// The free/paid/hold decision, shared by the router, the queue processor and
// the SMS page's pre-send check.
builder.Services.AddSingleton<ISmsRoutingPolicy, SmsRoutingPolicy>();
builder.Services.AddScoped<ISmsSender, TypeRoutedSmsSender>();
// Other senders still available to swap in directly instead of the router:
// LogOnlySmsSender (logs only), WasenderSmsSender (WhatsApp via WasenderAPI),
// ArsalSmsSender (WhatsApp via Arsal Solutions gateway)
// builder.Services.AddScoped<ISmsSender, LogOnlySmsSender>();
// builder.Services.AddScoped<ISmsSender, WasenderSmsSender>();
// builder.Services.AddScoped<ISmsSender, ArsalSmsSender>();
// Approved Meta template names per message type (appsettings MetaWhatsApp:Templates)
builder.Services.AddSingleton<ISmsTemplateCatalog, SmsTemplateCatalog>();
builder.Services.AddScoped<ISmsService, SmsService>();
// Two-way WhatsApp chat (inbound webhook + replies)
builder.Services.AddScoped<IWaChatRepository, WaChatRepository>();
builder.Services.AddScoped<IWhatsAppChatService, WhatsAppChatService>();
// Self-service menu: answers a parent's message with dues, voucher, ledger,
// result or attendance. Turn off with WhatsAppBot:Enabled = false.
builder.Services.AddScoped<IWaBotRepository, WaBotRepository>();
builder.Services.AddScoped<IWhatsAppBotService, WhatsAppBotService>();
builder.Services.AddSingleton<ISmsProgressTracker, SmsProgressTracker>();
// HTML->PDF renderer (shared headless Chromium) for WhatsApp result cards
builder.Services.AddSingleton<IPdfRenderer, PuppeteerPdfRenderer>();

// Settings
builder.Services.AddScoped<IBackupJobSettingRepository, BackupJobSettingRepository>();
builder.Services.AddScoped<IBackupJobSettingService, BackupJobSettingService>();
builder.Services.AddScoped<IGoogleDriveBackupUploader, GoogleDriveBackupUploader>();
builder.Services.AddScoped<IGoogleOAuthConnectService, GoogleOAuthConnectService>();

// Background Jobs
builder.Services.AddHostedService<AbsentNotificationJob>();
builder.Services.AddHostedService<FeeReminderJob>();
builder.Services.AddHostedService<SmsQueueProcessor>();
builder.Services.AddHostedService<DbBackupJob>();

// Controllers — registered ONCE with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Stamps UTC instants with a "Z" and writes calendar dates bare, so the
        // browser converts timestamps to the viewer's timezone while birthdays
        // and due dates stay put. See DateTimeJsonPolicy for the reasoning.
        options.JsonSerializerOptions.TypeInfoResolver =
            new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers = { School.API.Serialization.DateTimeJsonPolicy.Apply }
            };
    });

// Swagger, CORS & misc
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — SetIsOriginAllowed reflects the request Origin back in the
// Access-Control-Allow-Origin header instead of using a literal "*".
// Works correctly behind IIS/Nginx reverse proxies and avoids cache
// issues where intermediaries cache the "*" response for one origin
// and replay it for another.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("*");
    });
});

// Forwarded headers — required when deployed behind IIS/Nginx so that
// Request.Scheme / Request.Host reflect the PUBLIC url, not the
// upstream localhost. Without this, redirects and auth can break in
// production even though CORS seems to be the visible symptom.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
    // Trust the upstream proxy — clear the default restricted networks
    // so it works with any reverse-proxy topology.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ────────────────────────────────────────────────
// Order matters. Microsoft's documented order for CORS is:
//   ForwardedHeaders → ExceptionHandler → Routing → CORS → Auth → Endpoints
// Putting CORS AFTER UseRouting is REQUIRED so endpoint-level CORS
// attributes work and preflight OPTIONS requests are short-circuited
// by the CORS middleware before they ever hit authentication.

app.UseForwardedHeaders();

app.UseResponseCompression();

// Exception handler FIRST so every failure path (including ones from
// auth/routing) is caught. We manually re-attach the CORS headers here
// because an unhandled exception bypasses the CORS middleware's
// response writer.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Vary"] = "Origin";
        }
        else
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        }
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";

        var ex = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An internal server error occurred.\"}");
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

// CORS must be AFTER UseRouting and BEFORE UseAuthentication so that
// preflight OPTIONS requests are handled by the CORS middleware
// directly and never hit the auth pipeline (which would 401 them and
// the browser would report it as a CORS failure).
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();