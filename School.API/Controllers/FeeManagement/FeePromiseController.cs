using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.FeeManagement;
using School.API.Extensions;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Controllers.FeeManagement
{
    /// <summary>
    /// Promises to pay: a parent commits to clearing dues by a date, and until
    /// that date the student is off the unpaid list and gets no fee reminders.
    /// </summary>
    [Route("api/fee/promise")]
    [ApiController]
    public class FeePromiseController : ControllerBase
    {
        private readonly IFeePromiseService _service;

        public FeePromiseController(IFeePromiseService service)
        {
            _service = service;
        }

        /// <param name="status">0 all, 1 in force, 2 lapsed and still owing, 3 settled.</param>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? studentId,
            [FromQuery] string? classIds,
            [FromQuery] string? campusIds,
            [FromQuery] byte status = 0)
        {
            return Ok(await _service.GetAsync(null, studentId, classIds, campusIds, status));
        }

        /// <summary>
        /// The printed report: one row per STUDENT a promise covers, siblings
        /// included. The screen's own list is one row per promise, because its
        /// rows carry Edit and Delete; this one is what the office prints and
        /// works from, so every child the promise holds for has to be on it.
        /// </summary>
        /// <param name="status">0 all, 1 in force, 2 lapsed and still owing, 3 settled.</param>
        [HttpGet("report")]
        public async Task<IActionResult> GetReport(
            [FromQuery] string? classIds,
            [FromQuery] string? campusIds,
            [FromQuery] byte status = 0)
        {
            return Ok(await _service.GetReportAsync(classIds, campusIds, status));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rows = await _service.GetAsync(id, null, null, null, 0);
            var row = rows.FirstOrDefault();

            return row == null ? NotFound(new { message = "Promise not found" }) : Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveFeePromiseDto dto)
        {
            if (dto == null || dto.StudentID <= 0)
                return BadRequest(new { message = "Choose a student." });

            if (dto.PromiseDate == default)
                return BadRequest(new { message = "Enter the date the parent promised to pay by." });

            var userId = Request.GetUserId();
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var result = await _service.SaveAsync(dto, userId, userIp);

            // The procedure refuses a date already gone, and an unknown student.
            if (result != null && result.ReturnCode != 200)
                return BadRequest(new { message = result.ReturnMessage });

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = Request.GetUserId();
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var result = await _service.DeleteAsync(id, userId, userIp);
            return result == null ? NotFound() : Ok(result);
        }
    }
}
