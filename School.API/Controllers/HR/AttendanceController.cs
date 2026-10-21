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
            [FromQuery] int year, [FromQuery] int month, [FromQuery] int? designationId,
            [FromQuery] string? designationIds, [FromQuery] int? campusId, [FromQuery] string? sortBy)
        {
            if (year < 2000 || month < 1 || month > 12)
                return BadRequest(new { message = "Invalid year/month." });

            return Ok(await _service.GetMonthGridAsync(year, month, Designations(designationId, designationIds), campusId, sortBy));
        }

        // One date, every employee — with that date's school timings attached.
        [HttpGet("day")]
        public async Task<ActionResult<AttendanceDayGridDto>> GetDay(
            [FromQuery] DateTime date, [FromQuery] int? designationId,
            [FromQuery] string? designationIds, [FromQuery] int? campusId, [FromQuery] string? sortBy)
        {
            if (date == default)
                return BadRequest(new { message = "Please provide a date." });

            return Ok(await _service.GetDayGridAsync(date, Designations(designationId, designationIds), campusId, sortBy));
        }

        /// <summary>
        /// The designations to filter by, from either query parameter.
        /// </summary>
        /// <remarks>
        /// The screen sends `designationIds` as a comma-separated list, because
        /// the office picks several at once. `designationId` is the single-value
        /// parameter this endpoint used to take; it is still honoured so an old
        /// bookmark or a saved link does not quietly return everybody.
        /// Returns null for "no filter", which is not the same as an empty list
        /// from a cleared box — both mean everyone, and both arrive here.
        /// </remarks>
        private static List<int>? Designations(int? single, string? csv)
        {
            var ids = new List<int>();

            if (single is > 0) ids.Add(single.Value);

            if (!string.IsNullOrWhiteSpace(csv))
                ids.AddRange(csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(p => int.TryParse(p, out var n) ? n : 0)
                                .Where(n => n > 0));

            return ids.Count > 0 ? ids.Distinct().ToList() : null;
        }

        [HttpPost("bulk")]
        public async Task<ActionResult> SaveBulk(SaveAttendanceBulkDto dto)
        {
            var userId = Request.GetUserId();
            try
            {
                await _service.SaveBulkAsync(dto, userId == 0 ? null : userId);
            }
            catch (InvalidOperationException ex)
            {
                // Most often "this employee has no shift" — a setup problem the
                // user can fix, so say so instead of returning a bare 500.
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new { saved = dto.Cells.Count });
        }

        /// <summary>
        /// Works saved attendance out again against the timings as they stand
        /// now — after a day's hours were changed (an early closure), or to
        /// re-grade days the old rule turned into Half Days. Keeps every status
        /// a person chose; see AttendanceService.RecalculateAsync.
        /// </summary>
        [HttpPost("recalculate")]
        public async Task<ActionResult<AttendanceRecalculateResultDto>> Recalculate(RecalculateAttendanceDto dto)
        {
            if (dto == null || dto.From == default || dto.To == default || dto.To < dto.From)
                return BadRequest(new { message = "Give a date range: From on or before To." });
            if ((dto.To - dto.From).TotalDays > 62)
                return BadRequest(new { message = "Recalculate at most two months at a time." });

            var userId = Request.GetUserId();
            try
            {
                return Ok(await _service.RecalculateAsync(dto.From, dto.To, userId == 0 ? null : userId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
