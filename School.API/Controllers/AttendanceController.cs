using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;

        public AttendanceController(IAttendanceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AttendanceDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("date/{date}")]
        public async Task<ActionResult<IEnumerable<AttendanceListDto>>> GetByDate(DateTime date)
        {
            var result = await _service.GetByDateAsync(date);
            return Ok(result);
        }

        [HttpGet("class/{classId}/section/{sectionId}/session/{sessionId}/date/{date}")]
        public async Task<ActionResult<IEnumerable<AttendanceListDto>>> GetByClassSectionDate(int classId, int sectionId, int sessionId, DateTime date)
        {
            var result = await _service.GetByClassSectionDateAsync(classId, sectionId, sessionId, date);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<AttendanceListDto>>> GetByStudent(int studentId, int sessionId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            var result = await _service.GetByStudentAsync(studentId, sessionId, fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("summary/class/{classId}/section/{sectionId}/session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<AttendanceSummaryDto>>> GetSummary(int classId, int sectionId, int sessionId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var result = await _service.GetAttendanceSummaryAsync(classId, sectionId, sessionId, fromDate, toDate);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AttendanceDto>> Create(CreateAttendanceDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult> SaveBulk(BulkAttendanceDto dto)
        {
            await _service.SaveBulkAttendanceAsync(dto);
            return Ok(new { message = "Attendance saved successfully" });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AttendanceDto>> Update(int id, UpdateAttendanceDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }
    }
}
