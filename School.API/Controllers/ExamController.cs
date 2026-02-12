using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _service;

        public ExamController(IExamService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExamDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("class/{classId}/session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<ExamListDto>>> GetByClass(int classId, int sessionId)
        {
            var result = await _service.GetByClassAsync(classId, sessionId);
            return Ok(result);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<ExamDropdownDto>>> GetDropdown([FromQuery] int? classId = null, [FromQuery] int? sessionId = null)
        {
            var result = await _service.GetDropdownAsync(classId, sessionId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ExamDto>> Create(CreateExamDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ExamDto>> Update(int id, UpdateExamDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }

        // Exam Result endpoints
        [HttpGet("{examId}/results")]
        public async Task<ActionResult<IEnumerable<ExamResultListDto>>> GetResults(int examId)
        {
            var result = await _service.GetResultsByExamAsync(examId);
            return Ok(result);
        }

        [HttpGet("results/{id}")]
        public async Task<ActionResult<ExamResultDto>> GetResultById(int id)
        {
            var result = await _service.GetResultByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpPost("results")]
        public async Task<ActionResult<ExamResultDto>> CreateResult(CreateExamResultDto dto)
        {
            var result = await _service.CreateResultAsync(dto);
            return CreatedAtAction(nameof(GetResultById), new { id = result.Id }, result);
        }

        [HttpPost("results/bulk")]
        public async Task<ActionResult> SaveBulkResults(BulkExamResultDto dto)
        {
            await _service.SaveBulkResultsAsync(dto);
            return Ok(new { message = "Results saved successfully" });
        }

        [HttpDelete("results/{id}")]
        public async Task<ActionResult> DeleteResult(int id)
        {
            var success = await _service.DeleteResultAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }
    }
}
