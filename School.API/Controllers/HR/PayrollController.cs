using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/payroll")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _service;

        public PayrollController(IPayrollService service)
        {
            _service = service;
        }

        [HttpGet("runs")]
        public async Task<ActionResult<List<PayrollRunDto>>> GetRuns() => Ok(await _service.GetRunsAsync());

        [HttpPost("runs/generate")]
        public async Task<ActionResult<PayrollRunResultDto>> Generate(GeneratePayrollRunDto dto)
        {
            var userId = Request.GetUserId();
            try
            {
                return Ok(await _service.GenerateAsync(dto, userId == 0 ? null : userId));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("runs/{id}")]
        public async Task<ActionResult<List<PayslipDto>>> GetRunPayslips(int id) => Ok(await _service.GetPayslipsAsync(id));

        [HttpPut("runs/{id}/lock")]
        public async Task<ActionResult> LockRun(int id)
        {
            var userId = Request.GetUserId();
            var success = await _service.LockRunAsync(id, userId == 0 ? null : userId);
            return success ? Ok() : NotFound();
        }

        [HttpPut("runs/{id}/pay")]
        public async Task<ActionResult> PayRun(int id, PayRunDto dto)
        {
            var success = await _service.PayRunAsync(id, dto);
            return success ? Ok() : NotFound();
        }

        [HttpGet("payslip/{id}")]
        public async Task<ActionResult<PayslipDto>> GetPayslip(int id)
        {
            var result = await _service.GetPayslipAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("payslip/{id}/pay")]
        public async Task<ActionResult> PayEmployee(int id, PayEmployeeDto dto)
        {
            var success = await _service.PayEmployeeAsync(id, dto);
            return success ? Ok() : NotFound();
        }
    }
}
