using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    /// <summary>
    /// Read-only compatibility endpoint. Lists the employees flagged as
    /// teaching staff in the old SMSTeacher shape (VID / VName).
    /// </summary>
    /// <remarks>
    /// SMSTeacher was renamed into Employees, so a "teacher" is just an
    /// employee with IsTeachingStaff = 1, addressed by the same Id the academic
    /// tables have always used. Existing screens (Exam Reports, Staff Subjects)
    /// still fetch this route; new code should use api/employee instead.
    ///
    /// Save and delete are deliberately absent — staff are created and edited
    /// on the Employee screen, which is now the only place a staff row exists.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly IEmployeeService _service;

        public TeacherController(IEmployeeService service)
        {
            _service = service;
        }

        // GET api/teacher?examId=   (examId > 0 = only staff assigned in that exam)
        [HttpGet]
        public async Task<ActionResult<List<TeacherDto>>> GetAll([FromQuery] int examId = 0)
        {
            try
            {
                return Ok(await _service.GetTeachingStaffAsync(examId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load teaching staff.", error = ex.Message });
            }
        }

        // GET api/teacher/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDto>> GetById(int id)
        {
            try
            {
                var record = await _service.GetTeachingStaffByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"No teaching staff member with ID {id}." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load teaching staff member.", error = ex.Message });
            }
        }
    }
}
