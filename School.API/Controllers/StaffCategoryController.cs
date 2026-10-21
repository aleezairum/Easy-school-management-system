using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    /// <summary>
    /// Staff categories: the groups the office counts and prints staff by.
    /// Not designations — see Scripts/StaffCategory.sql.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StaffCategoryController : ControllerBase
    {
        private readonly IStaffCategoryService _service;

        public StaffCategoryController(IStaffCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffCategoryListDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffCategoryDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<IEnumerable<StaffCategoryDropdownDto>>> GetDropdown()
            => Ok(await _service.GetDropdownAsync());

        [HttpPost]
        public async Task<ActionResult<StaffCategoryDto>> Create(CreateStaffCategoryDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, null);
            if (error != null) return BadRequest(new { message = error });

            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StaffCategoryDto>> Update(int id, UpdateStaffCategoryDto dto)
        {
            var error = await ValidateAsync(dto.Code, dto.Name, id);
            if (error != null) return BadRequest(new { message = error });

            var result = await _service.UpdateAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            // Staff point at the category, so removing one still in use would
            // fail on the foreign key — say so instead of showing a 500.
            if (await _service.IsInUseAsync(id))
                return Conflict(new { message = "This category is assigned to staff. Move them first, or mark it inactive." });

            return await _service.DeleteAsync(id) ? NoContent() : NotFound();
        }

        private async Task<string?> ValidateAsync(string code, string name, int? id)
        {
            if (string.IsNullOrWhiteSpace(code)) return "Code is required.";
            if (string.IsNullOrWhiteSpace(name)) return "Name is required.";

            return await _service.CodeExistsAsync(code.Trim(), id)
                ? $"Category code '{code.Trim()}' already exists."
                : null;
        }
    }
}
