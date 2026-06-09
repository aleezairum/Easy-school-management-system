using Microsoft.AspNetCore.Mvc;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly ICertificateService _service;

        public CertificateController(ICertificateService service)
        {
            _service = service;
        }

        [HttpGet("leaving/{studentId}")]
        public async Task<IActionResult> GetLeaving(
            int studentId,
            [FromQuery] DateTime? leavingDate = null,
            [FromQuery] string? examination = null,
            [FromQuery] string? marksObtained = null,
            [FromQuery] string? totalMarks = null)
        {
            var record = await _service.GetLeavingAsync(studentId, leavingDate, examination, marksObtained, totalMarks);
            return record == null ? NotFound() : Ok(record);
        }

        [HttpGet("provisional/{studentId}")]
        public async Task<IActionResult> GetProvisional(
            int studentId,
            [FromQuery] int? sessionId = null,
            [FromQuery] string? boardRollNo = null,
            [FromQuery] string? examRollNo = null,
            [FromQuery] string? examination = null,
            [FromQuery] string? marksObtained = null,
            [FromQuery] string? totalMarks = null,
            [FromQuery] string? conduct = null,
            [FromQuery] string? moralCharacter = null,
            [FromQuery] DateTime? issueDate = null)
        {
            var record = await _service.GetProvisionalAsync(
                studentId, sessionId, boardRollNo, examRollNo, examination,
                marksObtained, totalMarks, conduct, moralCharacter, issueDate);
            return record == null ? NotFound() : Ok(record);
        }
    }
}
