using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HRGradeController : ControllerBase
    {
        private readonly IHRGradeService _service;

        public HRGradeController(IHRGradeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HRGradeListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HRGradeDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<HRGradeDropdownDto>>> GetDropdown()
        {
            var result = await _service.GetDropdownAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<HRGradeDto>> Create(CreateHRGradeDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, dto.MinSalary, dto.MaxSalary, null);
            if (error != null)
                return BadRequest(new { message = error });

            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HRGradeDto>> Update(int id, UpdateHRGradeDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, dto.MinSalary, dto.MaxSalary, id);
            if (error != null)
                return BadRequest(new { message = error });

            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            // Employees point at the grade, so removing one still in use would
            // fail on the foreign key — say so instead.
            if (await _service.IsInUseAsync(id))
                return Conflict(new { message = "This grade is assigned to employees. Reassign them first, or mark it inactive." });

            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }

        private async Task<string?> ValidateAsync(string code, string name, decimal? minSalary, decimal? maxSalary, int? id)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Code is required.";
            if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
            if (minSalary is < 0 || maxSalary is < 0) return "Salary cannot be negative.";
            if (minSalary.HasValue && maxSalary.HasValue && minSalary > maxSalary)
                return "Min salary cannot be greater than max salary.";

            return await _service.CodeExistsAsync(code.Trim(), id)
                ? $"Grade code '{code.Trim()}' already exists."
                : null;
        }
    }
}
