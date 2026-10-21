using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/school-timings")]
    public class SchoolTimingController : ControllerBase
    {
        private readonly ISchoolTimingService _service;
        private readonly IAttendanceService _attendance;

        public SchoolTimingController(ISchoolTimingService service, IAttendanceService attendance)
        {
            _service = service;
            _attendance = attendance;
        }

        /// <summary>
        /// Changing ONE day's hours has to change the attendance already saved
        /// for that day: the minutes are stored when a day is saved, so without
        /// this an early closure at 12:45 went on charging everyone for leaving
        /// "early". Returns what the re-grade touched, for the screen to report.
        /// </summary>
        private async Task<AttendanceRecalculateResultDto> RegradeDayAsync(DateTime date)
        {
            var userId = Request.GetUserId();
            return await _attendance.RecalculateAsync(date, date, userId == 0 ? null : userId);
        }

        [HttpGet]
        public async Task<ActionResult<List<SchoolTimingDto>>> GetAll([FromQuery] int? year) =>
            Ok(await _service.GetAllAsync(year));

        [HttpGet("{id}")]
        public async Task<ActionResult<SchoolTimingDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // The individual days a range resolved to — holidays are absent from it.
        [HttpGet("{id}/days")]
        public async Task<ActionResult<List<SchoolTimingDayDto>>> GetDays(int id)
        {
            var timing = await _service.GetByIdAsync(id);
            if (timing == null) return NotFound();

            return Ok(await _service.GetDaysAsync(id));
        }

        // The individual days a range works out to, without saving anything —
        // holidays dropped, Fridays on the Friday times. The screen lists these
        // for editing before the user commits to them.
        [HttpGet("expand")]
        public async Task<ActionResult<List<SchoolTimingDayDto>>> Expand(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] TimeSpan timeIn,
            [FromQuery] TimeSpan timeOut,
            [FromQuery] TimeSpan? fridayTimeIn,
            [FromQuery] TimeSpan? fridayTimeOut,
            [FromQuery] byte? offDay1,
            [FromQuery] byte? offDay2)
        {
            try
            {
                return Ok(await _service.ExpandAsync(
                    startDate, endDate, timeIn, timeOut, fridayTimeIn, fridayTimeOut, offDay1, offDay2));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<SchoolTimingDto>> Create(SaveSchoolTimingDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto, Request.GetUserId());
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SchoolTimingDto>> Update(int id, SaveSchoolTimingDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto, Request.GetUserId());
                return result == null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        // Change ONE day. Fridays and one-off changes are the two things a flat
        // range cannot express, so the day rows are editable in their own right.
        [HttpPut("days/{dayId}")]
        public async Task<ActionResult<SchoolTimingDayDto>> UpdateDay(int dayId, SaveSchoolTimingDayTimeDto dto)
        {
            try
            {
                var result = await _service.UpdateDayAsync(dayId, dto);
                if (result == null) return NotFound();

                var regraded = await RegradeDayAsync(result.TDate);
                return Ok(new { day = result, regraded });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Put one hand-edited day back on whatever its range says it should be.
        [HttpPost("days/{dayId}/reset")]
        public async Task<ActionResult<SchoolTimingDayDto>> ResetDay(int dayId)
        {
            var result = await _service.ResetDayAsync(dayId);
            if (result == null) return NotFound();

            var regraded = await RegradeDayAsync(result.TDate);
            return Ok(new { day = result, regraded });
        }
    }
}
