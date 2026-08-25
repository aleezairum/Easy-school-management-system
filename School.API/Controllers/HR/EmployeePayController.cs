using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/employee-pay")]
    public class EmployeePayController : ControllerBase
    {
        private readonly IEmployeePayService _service;

        public EmployeePayController(IEmployeePayService service)
        {
            _service = service;
        }

        [HttpGet("{employeeId}")]
        public async Task<ActionResult<List<EmployeePayDto>>> GetHistory(int employeeId) =>
            Ok(await _service.GetHistoryAsync(employeeId));

        [HttpPost]
        public async Task<ActionResult<EmployeePayDto>> Create(SaveEmployeePayDto dto) =>
            Ok(await _service.CreateAsync(dto));
    }
}
