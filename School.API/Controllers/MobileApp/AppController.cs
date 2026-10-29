using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.MobileApp;
using School.API.Extensions;
using School.API.Services.Implementations.MobileApp;
using School.API.Services.Interfaces;

namespace School.API.Controllers.MobileApp
{
    /// <summary>
    /// The EasySchool mobile app's API. Everything but sign-in and the school's
    /// name needs an app token (policy "App" - tokens from anywhere else in the
    /// API are refused), and every answer is limited on the server to what that
    /// login may see (AppService).
    /// </summary>
    [ApiController]
    [Route("api/app")]
    [Authorize(Policy = AppClaims.Policy)]
    public class AppController : ControllerBase
    {
        private readonly AppService _app;
        private readonly AppCallerResolver _callers;

        public AppController(AppService app, AppCallerResolver callers)
        {
            _app = app;
            _callers = callers;
        }

        // The signed-in caller, or a 401 when the login was switched off since the token was issued.
        private async Task<(AppCaller? Caller, ActionResult? Error)> CallerAsync()
        {
            var c = await _callers.ResolveAsync(User);
            return c == null ? (null, Unauthorized(new { message = "Please sign in again." })) : (c, null);
        }

        private async Task<ActionResult> Run<T>(Func<AppCaller, Task<T>> work)
        {
            var (c, err) = await CallerAsync();
            if (err != null) return err;
            try { return Ok(await work(c!)); }
            catch (AppForbiddenException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (KeyNotFoundException) { return NotFound(new { message = "Not found." }); }
        }

        private async Task<ActionResult> RunFile(Func<AppCaller, Task<(byte[]? Data, string Type, string? Name, string? Note)>> work)
        {
            var (c, err) = await CallerAsync();
            if (err != null) return err;
            try
            {
                var (data, type, name, note) = await work(c!);
                if (data == null || data.Length == 0) return NotFound(new { message = note ?? "Not available yet." });
                return name == null ? File(data, type) : File(data, type, name);
            }
            catch (AppForbiddenException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (KeyNotFoundException) { return NotFound(new { message = "Not found." }); }
        }

        private static (byte[]?, string, string?, string?) Pdf(BotOutgoing? d, string? note = null) =>
            (d?.DocumentBytes, "application/pdf", d?.DocumentFileName, note);

        // ── sign in ───────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpGet("school-info")]
        public ActionResult<AppSchoolInfoDto> SchoolInfo() => _app.SchoolInfo();

        [AllowAnonymous]
        [HttpPost("auth/login")]
        public async Task<ActionResult> Login(AppLoginDto dto)
        {
            var (result, error) = await _app.LoginAsync(dto);
            return result == null ? Unauthorized(new { message = error }) : Ok(result);
        }

        [HttpPost("auth/change-password")]
        public async Task<ActionResult> ChangePassword(AppChangePasswordDto dto)
        {
            var (c, err) = await CallerAsync();
            if (err != null) return err;
            var problem = await _app.ChangePasswordAsync(c!, dto);
            return problem == null ? Ok(new { message = "Password changed." }) : BadRequest(new { message = problem });
        }

        [HttpGet("me")]
        public Task<ActionResult> Me() => Run(c => _app.MeAsync(c));

        [HttpGet("announcements")]
        public Task<ActionResult> Announcements() => Run(c => _app.AnnouncementsAsync(c));

        /// <summary>Publish an announcement from the app ("Send announcements"); it is pushed straight away.</summary>
        [HttpPost("announcements")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<ActionResult> PostAnnouncement(AppPostAnnouncementDto dto)
        {
            var (c, err) = await CallerAsync();
            if (err != null) return err;
            try { return Ok(await _app.PostAnnouncementAsync(c!, dto)); }
            catch (AppForbiddenException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── chat: the WhatsApp self-service menu inside the app ───────

        /// <summary>Send "hi", a menu number or a picked row id; the replies come back as chat bubbles.</summary>
        [HttpPost("chat")]
        public Task<ActionResult> Chat(AppChatSendDto dto, [FromServices] AppChatService chat) =>
            Run(c => chat.SendAsync(c, dto.Text));

        /// <summary>A PDF or picture from a chat reply (kept 30 minutes).</summary>
        [HttpGet("chat/files/{id}")]
        public Task<ActionResult> ChatFile(string id, [FromServices] AppChatService chat) =>
            RunFile(c => Task.FromResult(chat.File(c, id) is { } f
                ? ((byte[]?)f.Data, f.Type, (string?)f.Name, (string?)null)
                : ((byte[]?)null, "", (string?)null, (string?)"This file has expired - ask again in the chat.")));

        [HttpGet("announcements/{id:int}/image")]
        public Task<ActionResult> AnnouncementImage(int id) =>
            RunFile(async c => (await _app.AnnouncementImageAsync(c, id)) is { } i ? (i.Data, i.ContentType, null, null) : (null, "", null, null));

        // ── a student (student, parent, or staff allowed to see them) ──

        [HttpGet("students/{id:int}/summary")]
        public Task<ActionResult> StudentSummary(int id) => Run(c => _app.StudentSummaryAsync(c, id));

        [HttpGet("students/{id:int}/attendance")]
        public Task<ActionResult> StudentAttendance(int id, [FromQuery] int? year, [FromQuery] int? month) =>
            Run(c => _app.AttendanceMonthAsync(c, id, year ?? DateTime.Today.Year, month ?? DateTime.Today.Month));

        [HttpGet("students/{id:int}/fees")]
        public Task<ActionResult> StudentFees(int id) => Run(c => _app.FeesAsync(c, id));

        [HttpGet("students/{id:int}/ledger")]
        public Task<ActionResult> StudentLedger(int id) => Run(c => _app.LedgerAsync(c, id));

        [HttpGet("students/{id:int}/ledger.pdf")]
        public Task<ActionResult> StudentLedgerPdf(int id) => RunFile(async c => Pdf(await _app.LedgerPdfAsync(c, id), "No ledger entries yet."));

        [HttpGet("students/{id:int}/voucher.pdf")]
        public Task<ActionResult> StudentVoucherPdf(int id) => RunFile(async c => Pdf(await _app.VoucherPdfAsync(c, id), "No fee voucher recorded yet."));

        [HttpGet("students/{id:int}/exams")]
        public Task<ActionResult> StudentExams(int id) => Run(c => _app.ExamsAsync(c, id));

        [HttpGet("students/{id:int}/exams/{examId:int}")]
        public Task<ActionResult> StudentExam(int id, int examId) => Run(c => _app.ExamSubjectsAsync(c, id, examId));

        [HttpGet("students/{id:int}/exams/{examId:int}/card.pdf")]
        public Task<ActionResult> StudentResultCard(int id, int examId) =>
            RunFile(async c => Pdf(await _app.ResultCardPdfAsync(c, id, examId), "No published result for this exam."));

        // ── results for staff: every finalised result, read only ──

        [HttpGet("results/exams")]
        public Task<ActionResult> ResultExams() => Run(c => _app.ResultExamsAsync(c));

        [HttpGet("results/{examId:int}/sections")]
        public Task<ActionResult> ResultSections(int examId) => Run(c => _app.ResultSectionsAsync(c, examId));

        [HttpGet("results/{examId:int}/sections/{sectionId:int}")]
        public Task<ActionResult> SectionResult(int examId, int sectionId) => Run(c => _app.SectionResultAsync(c, examId, sectionId));

        [HttpGet("students/{id:int}/diary")]
        public Task<ActionResult> StudentDiary(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
            Run(c => _app.DiaryAsync(c, id, (from ?? DateTime.Today.AddDays(-6)).Date, (to ?? DateTime.Today).Date));

        [HttpGet("diary-images/{imageId:int}")]
        public Task<ActionResult> DiaryImage(int imageId) =>
            RunFile(async c => (await _app.DiaryImageAsync(c, imageId)) is { } i ? (i.Data, i.ContentType, null, null) : (null, "", null, null));

        [HttpGet("students/{id:int}/timing")]
        public Task<ActionResult> StudentTiming(int id) => Run(c => _app.TimingAsync(c, id));

        // ── staff ─────────────────────────────────────────────────────

        [HttpGet("staff/summary")]
        public Task<ActionResult> StaffSummary() => Run(c => _app.StaffSummaryAsync(c));

        [HttpGet("staff/attendance")]
        public Task<ActionResult> StaffAttendance([FromQuery] int? year, [FromQuery] int? month) =>
            Run(c => _app.StaffMonthAsync(c, year ?? DateTime.Today.Year, month ?? DateTime.Today.Month));

        [HttpGet("staff/payslips")]
        public Task<ActionResult> StaffPayslips() => Run(c => _app.PayslipsAsync(c));

        [HttpGet("staff/payslips/{year:int}/{month:int}.pdf")]
        public Task<ActionResult> StaffPayslipPdf(int year, int month) =>
            RunFile(async c => { var (d, n) = await _app.StaffDocumentAsync(c, "slip", year, month); return Pdf(d, n); });

        [HttpGet("staff/attendance-card/{year:int}/{month:int}.pdf")]
        public Task<ActionResult> StaffAttendanceCard(int year, int month) =>
            RunFile(async c => { var (d, n) = await _app.StaffDocumentAsync(c, "card", year, month); return Pdf(d, n); });

        [HttpGet("staff/classes")]
        public Task<ActionResult> StaffClasses() => Run(c => _app.ClassesAsync(c));

        [HttpGet("classes/{sectionId:int}/attendance")]
        public Task<ActionResult> ClassAttendance(int sectionId, [FromQuery] DateTime? date) =>
            Run(c => _app.ClassAttendanceAsync(c, sectionId, (date ?? DateTime.Today).Date));

        [HttpPost("classes/{sectionId:int}/attendance")]
        public Task<ActionResult> SaveClassAttendance(int sectionId, AppSaveClassAttendanceDto dto) =>
            Run(async c => new { saved = await _app.SaveClassAttendanceAsync(c, sectionId, dto, 0, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "app") });

        // ── admin ─────────────────────────────────────────────────────

        [HttpGet("admin/students")]
        public Task<ActionResult> SearchStudents([FromQuery] string q) => Run(c => _app.SearchStudentsAsync(c, q));

        [HttpGet("admin/dashboard")]
        public async Task<IActionResult> AdminDashboard([FromServices] IServiceProvider sp)
        {
            var (c, err) = await CallerAsync();
            if (err != null) return err;
            if (!c!.Can(School.API.Data.DBModels.MobileApp.AppFeatures.AdminDashboard))
                return StatusCode(403, new { message = "Your role cannot see the school dashboard." });
            // The web dashboard's own figures, so the app and the office never disagree.
            var dash = ActivatorUtilities.CreateInstance<School.API.Controllers.DashboardController>(sp);
            dash.ControllerContext = ControllerContext;
            return await dash.GetDashboard();
        }

        // ── push ──────────────────────────────────────────────────────

        [HttpPost("devices")]
        public Task<ActionResult> RegisterDevice(AppDeviceDto dto) => Run(async c => { await _app.RegisterDeviceAsync(c, dto); return new { ok = true }; });

        [HttpDelete("devices")]
        public Task<ActionResult> RemoveDevice([FromQuery] string token) => Run(async c => { await _app.RemoveDeviceAsync(c, token); return new { ok = true }; });
    }
}
