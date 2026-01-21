using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallanVoucherController : ControllerBase
    {
        private readonly IChallanVoucherService _service;

        public ChallanVoucherController(IChallanVoucherService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChallanVoucherListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChallanVoucherDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<ChallanVoucherListDto>>> GetByStudent(int studentId)
        {
            var result = await _service.GetByStudentAsync(studentId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/section/{sectionId}/session/{sessionId}")]
        public async Task<ActionResult<IEnumerable<ChallanVoucherListDto>>> GetByClassSection(int classId, int sectionId, int sessionId)
        {
            var result = await _service.GetByClassSectionAsync(classId, sectionId, sessionId);
            return Ok(result);
        }

        [HttpGet("pending/student/{studentId}")]
        public async Task<ActionResult<IEnumerable<ChallanDropdownDto>>> GetPendingByStudent(int studentId)
        {
            var result = await _service.GetPendingByStudentAsync(studentId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ChallanVoucherDto>> Create(CreateChallanVoucherDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ChallanVoucherDto>> Update(int id, UpdateChallanVoucherDto dto)
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
    }
}
