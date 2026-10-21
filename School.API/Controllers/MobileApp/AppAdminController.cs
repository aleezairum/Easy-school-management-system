using Microsoft.AspNetCore.Mvc;
using School.API.Data.DBModels.MobileApp;
using School.API.Extensions;
using School.API.Services.Implementations.MobileApp;

namespace School.API.Controllers.MobileApp
{
    /// <summary>
    /// The office's screens for the mobile app: App Logins (students and staff)
    /// and Mobile App Permissions (what each role may do in the app).
    /// </summary>
    [ApiController]
    [Route("api/app-admin")]
    public class AppAdminController : ControllerBase
    {
        private readonly AppAdminService _svc;
        public AppAdminController(AppAdminService svc) => _svc = svc;

        private int? UserId => Request.GetUserId() is int u && u > 0 ? u : null;

        [HttpGet("features")]
        public ActionResult Features() => Ok(AppFeatures.All.Select(f => new { key = f.Key, title = f.Title, description = f.Description }));

        [HttpGet("students")]
        public async Task<ActionResult> Students([FromQuery] int sectionId) =>
            sectionId > 0 ? Ok(await _svc.StudentRowsAsync(sectionId)) : BadRequest(new { message = "Choose a section." });

        [HttpPost("students/generate")]
        public async Task<ActionResult> Generate(AppGenerateLoginsDto dto) =>
            Ok(new { made = await _svc.GenerateStudentLoginsAsync(dto.StudentIds, UserId) });

        [HttpGet("staff")]
        public async Task<ActionResult> Staff([FromQuery] int? campusId) => Ok(await _svc.StaffRowsAsync(campusId));

        [HttpPost("staff")]
        public async Task<ActionResult> SaveStaff(AppStaffLoginDto dto)
        {
            try { return Ok(await _svc.SaveStaffLoginAsync(dto, UserId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPost("accounts/{id:int}/reset")]
        public async Task<ActionResult> Reset(int id)
        {
            try { return Ok(new { password = await _svc.ResetAsync(id) }); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPut("accounts/{id:int}/active")]
        public async Task<ActionResult> Active(int id, [FromQuery] bool value)
        {
            try { await _svc.SetActiveAsync(id, value); return Ok(new { ok = true }); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("usage")]
        public async Task<ActionResult> Usage([FromQuery] int? campusId) => Ok(await _svc.UsageAsync(campusId > 0 ? campusId : null));

        [HttpGet("roles")]
        public async Task<ActionResult> Roles() => Ok(await _svc.RolesAsync());

        [HttpPut("roles/{roleId:int}/features")]
        public async Task<ActionResult> RoleFeatures(int roleId, [FromBody] List<string> keys)
        {
            try { await _svc.SetRoleFeaturesAsync(roleId, keys ?? new()); return Ok(new { ok = true }); }
            catch (KeyNotFoundException) { return NotFound(); }
        }
    }
}
