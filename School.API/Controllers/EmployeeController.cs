using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;

        public EmployeeController(IEmployeeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<EmployeeListDto>>> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("dropdown")]
        public async Task<ActionResult<List<EmployeeDropdownDto>>> GetDropdown() => Ok(await _service.GetDropdownAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> Create(SaveEmployeeDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto, Request.GetUserId(), CallerIp());
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> Update(int id, SaveEmployeeDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto, Request.GetUserId(), CallerIp());
                return result == null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string CallerIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                return success ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
