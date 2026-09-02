using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffSubjectController : ControllerBase
    {
        private readonly IStaffSubjectService _service;

        public StaffSubjectController(IStaffSubjectService service)
        {
            _service = service;
        }

        // GET api/staffsubject?staffId=&classId=&sectionId=&examId=  (all by default)
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int staffId = 0,
            [FromQuery] int classId = 0,
            [FromQuery] int sectionId = 0,
            [FromQuery] int examId = 0)
        {
            try
            {
                var rows = await _service.GetAsync(staffId, classId, sectionId, examId);
                return Ok(rows);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load staff subjects.", error = ex.Message });
            }
        }

        // POST api/staffsubject  (VID = 0 -> insert, VID > 0 -> update)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] StaffSubjectDto dto)
        {
            try
            {
                if (dto == null || dto.ExamId <= 0)
                    return BadRequest(new { message = "Please select an exam." });

                if (dto.StaffId <= 0)
                    return BadRequest(new { message = "Please select a staff member." });

                if (dto.ClassId <= 0 || dto.SectionId <= 0 || dto.SubjectId <= 0)
                    return BadRequest(new { message = "Please select Class, Section and Subject." });

                int userId = Request.GetUserId();
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                var result = await _service.SaveAsync(dto, userId, userIp);

                // The procedure rejects a duplicate or an unknown exam with 400 —
                // surface that as a bad request, not as a "saved" response.
                if (result != null && result.ReturnCode != 200)
                    return BadRequest(new { message = result.ReturnMessage });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to save staff subject.", error = ex.Message });
            }
        }

        // POST api/staffsubject/copy  — copy every assignment of one exam into another
        [HttpPost("copy")]
        public async Task<IActionResult> Copy([FromBody] CopyStaffSubjectDto dto)
        {
            try
            {
                if (dto == null || dto.FromExamId <= 0)
                    return BadRequest(new { message = "Please select the exam to copy from." });

                if (dto.ToExamId <= 0)
                    return BadRequest(new { message = "Please select the exam to copy into." });

                if (dto.FromExamId == dto.ToExamId)
                    return BadRequest(new { message = "Source and target exam cannot be the same." });

                int userId = Request.GetUserId();
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                var result = await _service.CopyAsync(dto, userId, userIp);

                if (result == null)
                    return StatusCode(500, new { message = "Copy returned no result." });

                if (result.ReturnCode != 200)
                    return BadRequest(new { message = result.ReturnMessage });

                return Ok(new { copied = (int)result.VID, message = result.ReturnMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to copy staff subjects.", error = ex.Message });
            }
        }

        // DELETE api/staffsubject/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            try
            {
                var record = await _service.DeleteByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Staff subject with ID {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete staff subject.", error = ex.Message });
            }
        }
    }
}
