using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DesignationController : ControllerBase
    {
        private readonly IDesignationService _service;

        public DesignationController(IDesignationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DesignationListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DesignationDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<DesignationDropdownDto>>> GetDropdown()
        {
            var result = await _service.GetDropdownAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<DesignationDto>> Create(CreateDesignationDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, null);
            if (error != null)
                return BadRequest(new { message = error });

            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DesignationDto>> Update(int id, UpdateDesignationDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, id);
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
            // Employees point at the designation, so removing one still in use
            // would fail on the foreign key — say so instead.
            if (await _service.IsInUseAsync(id))
                return Conflict(new { message = "This designation is assigned to employees. Reassign them first, or mark it inactive." });

            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound();
            return NoContent();
        }

        private async Task<string?> ValidateAsync(string code, string name, int? id)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Code is required.";
            if (string.IsNullOrWhiteSpace(name)) return "Name is required.";

            return await _service.CodeExistsAsync(code.Trim(), id)
                ? $"Designation code '{code.Trim()}' already exists."
                : null;
        }
    }
}
