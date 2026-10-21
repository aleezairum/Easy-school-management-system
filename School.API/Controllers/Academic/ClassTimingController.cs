using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    /// <summary>
    /// Class-wise school timing for students.
    /// </summary>
    /// <remarks>
    /// Separate from the HR school-timing endpoints on purpose: those describe
    /// the staff working day and feed attendance and payroll. This one is what
    /// parents are told their child's day looks like, and the WhatsApp
    /// auto-reply reads it straight out of the same table.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class ClassTimingController : ControllerBase
    {
        private readonly IClassTimingService _service;

        public ClassTimingController(IClassTimingService service)
        {
            _service = service;
        }

        /// <param name="campusId">
        /// The editing grid passes a campus and gets every active class back,
        /// blanks included. 0 returns only the rows already saved.
        /// </param>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int campusId = 0)
            => Ok(await _service.GetAllAsync(campusId));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var row = await _service.GetByIdAsync(id);
            return row is null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Save(SaveClassTimingDto dto)
        {
            var result = await _service.SaveAsync(dto, Request.GetUserId(), ClientIp());
            return Ok(result);
        }

        /// <summary>Saves the whole campus in one go — what the screen posts.</summary>
        [HttpPost("batch")]
        public async Task<IActionResult> SaveBatch(SaveClassTimingBatchDto batch)
        {
            if (batch.CampusID <= 0)
                return BadRequest(new { message = "Campus is required." });

            var result = await _service.SaveBatchAsync(batch, Request.GetUserId(), ClientIp());
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        private string ClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
