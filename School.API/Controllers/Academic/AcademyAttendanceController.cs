using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    /// <summary>Students -> Mark Academy Attendance.</summary>
    [ApiController]
    [Route("api/academy-attendance")]
    public class AcademyAttendanceController : ControllerBase
    {
        private readonly IAcademyAttendanceService _service;

        public AcademyAttendanceController(IAcademyAttendanceService service)
        {
            _service = service;
        }

        private int? UserId => Request.GetUserId() is var u && u > 0 ? u : null;

        /// <param name="sectionId">Leave out for every section of the class.</param>
        [HttpGet]
        public async Task<ActionResult<List<AcademyAttendanceRowDto>>> Get([FromQuery] int classId, [FromQuery] int? sectionId, [FromQuery] DateTime date)
        {
            try { return Ok(await _service.GetAsync(classId, sectionId > 0 ? sectionId : null, date)); }
            catch (SqlException ex) when (ex.Number == 208) { return TableMissing(); }
        }

        [HttpPost]
        public async Task<ActionResult> Save(SaveAcademyAttendanceDto dto)
        {
            try { return Ok(new { saved = await _service.SaveAsync(dto, UserId) }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (SqlException ex) when (ex.Number == 208) { return TableMissing(); }
        }

        [HttpDelete]
        public async Task<ActionResult> Delete([FromQuery] int classId, [FromQuery] int? sectionId, [FromQuery] DateTime date)
        {
            try { return Ok(new { removed = await _service.DeleteAsync(classId, sectionId > 0 ? sectionId : null, date) }); }
            catch (SqlException ex) when (ex.Number == 208) { return TableMissing(); }
        }

        private ObjectResult TableMissing() =>
            StatusCode(500, new { message = "The academy attendance table is not in the database yet: run Scripts/AcademyAttendance.sql." });
    }
}
