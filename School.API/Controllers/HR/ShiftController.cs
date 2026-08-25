using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/shifts")]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _service;

        public ShiftController(IShiftService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ShiftDto>>> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("dropdown")]
        public async Task<ActionResult<List<ShiftDropdownDto>>> GetDropdown() => Ok(await _service.GetDropdownAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<ShiftDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ShiftDto>> Create(SaveShiftDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ShiftDto>> Update(int id, SaveShiftDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
