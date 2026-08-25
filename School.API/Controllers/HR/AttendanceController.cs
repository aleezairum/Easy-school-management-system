using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;

        public AttendanceController(IAttendanceService service)
        {
            _service = service;
        }

        [HttpGet("month")]
        public async Task<ActionResult<AttendanceMonthGridDto>> GetMonth(
            [FromQuery] int year, [FromQuery] int month, [FromQuery] int? designationId)
        {
            if (year < 2000 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid year/month." });

            return Ok(await _service.GetMonthGridAsync(year, month, designationId));
        }

        [HttpPost("bulk")]
        public async Task<ActionResult> SaveBulk(SaveAttendanceBulkDto dto)
        {
            var userId = Request.GetUserId();
            await _service.SaveBulkAsync(dto, userId == 0 ? null : userId);
            return Ok(new { saved = dto.Cells.Count });
        }

        [HttpPost("fill")]
        public async Task<ActionResult> Fill(FillAttendanceDto dto)
        {
            var userId = Request.GetUserId();
            await _service.FillAsync(dto, userId == 0 ? null : userId);
            return Ok();
        }
    }
}
