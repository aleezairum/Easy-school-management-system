using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    /// <summary>
    /// Academy Subject Allocation: which staff member takes which subject for the
    /// academy students of a class (or one section of it), per exam. Feeds the
    /// Academy Teacher Wise result (examresult/academy-teacher-wise-report).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AcademyStaffSubjectController : ControllerBase
    {
        private readonly IAcademyStaffSubjectService _service;

        public AcademyStaffSubjectController(IAcademyStaffSubjectService service)
        {
            _service = service;
        }

        // GET api/academystaffsubject?staffId=&classId=&sectionId=&examId=  (all by default)
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int staffId = 0,
            [FromQuery] int classId = 0,
            [FromQuery] int sectionId = 0,
            [FromQuery] int examId = 0)
        {
            try
            {
                return Ok(await _service.GetAsync(staffId, classId, sectionId, examId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load the academy allocation.", error = ex.Message });
            }
        }

        // POST api/academystaffsubject  (VID = 0 -> insert, VID > 0 -> update; SectionId 0 = all sections)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] StaffSubjectDto dto)
        {
            try
            {
                if (dto == null || dto.ExamId <= 0)
                    return BadRequest(new { message = "Please select an exam." });

                if (dto.StaffId <= 0)
                    return BadRequest(new { message = "Please select a staff member." });

                if (dto.ClassId <= 0 || dto.SubjectId <= 0)
                    return BadRequest(new { message = "Please select Class and Subject." });

                int userId = Request.GetUserId();
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                var result = await _service.SaveAsync(dto, userId, userIp);
                if (result != null && result.ReturnCode != 200)
                    return BadRequest(new { message = result.ReturnMessage });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to save the academy allocation.", error = ex.Message });
            }
        }

        // POST api/academystaffsubject/copy  — copy one exam's academy allocation into another
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
                return StatusCode(500, new { message = "Failed to copy the academy allocation.", error = ex.Message });
            }
        }

        // DELETE api/academystaffsubject/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            try
            {
                var record = await _service.DeleteByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Academy allocation {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete the academy allocation.", error = ex.Message });
            }
        }
    }
}
