using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherService _service;

        public TeacherController(ITeacherService service)
        {
            _service = service;
        }

        // GET api/teacher
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load teachers.", error = ex.Message });
            }
        }

        // GET api/teacher/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var record = await _service.GetByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Teacher with ID {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load teacher.", error = ex.Message });
            }
        }

        // DELETE api/teacher/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            try
            {
                var record = await _service.DeleteByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Teacher with ID {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete teacher.", error = ex.Message });
            }
        }

        // POST api/teacher  (VID = 0 → Insert, VID > 0 → Update)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] TeacherDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is empty." });

                if (string.IsNullOrWhiteSpace(dto.VName))
                    return BadRequest(new { message = "Name is required." });

                int userId = Request.GetUserId();
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                var result = await _service.SaveAsync(dto, userId, userIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to save teacher.", error = ex.Message });
            }
        }
    }
}
