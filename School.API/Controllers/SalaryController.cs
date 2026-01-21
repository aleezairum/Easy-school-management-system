using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryController : ControllerBase
    {
        private readonly ISalaryService _service;

        public SalaryController(ISalaryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaryListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalaryDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("month/{month}/year/{year}")]
        public async Task<ActionResult<IEnumerable<SalaryListDto>>> GetByMonthYear(int month, int year)
        {
            var result = await _service.GetByMonthYearAsync(month, year);
            return Ok(result);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<SalaryListDto>>> GetByEmployee(int employeeId)
        {
            var result = await _service.GetByEmployeeAsync(employeeId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<SalaryDto>> Create(CreateSalaryDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SalaryDto>> Update(int id, UpdateSalaryDto dto)
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

        [HttpPost("approve")]
        public async Task<ActionResult> Approve(ApproveSalaryDto dto)
        {
            await _service.ApproveSalariesAsync(dto.SalaryIds);
            return Ok(new { message = "Salaries approved successfully" });
        }

        [HttpPost("pay")]
        public async Task<ActionResult> Pay(PaySalaryDto dto)
        {
            await _service.PaySalaryAsync(dto);
            return Ok(new { message = "Salary paid successfully" });
        }
    }
}
