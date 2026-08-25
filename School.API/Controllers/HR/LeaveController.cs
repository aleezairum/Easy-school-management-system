using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/leaves")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _service;

        public LeaveController(ILeaveService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<LeaveDto>>> GetAll([FromQuery] byte? status) =>
            Ok(await _service.GetAllAsync(status));

        [HttpPost]
        public async Task<ActionResult<LeaveDto>> Apply(ApplyLeaveDto dto) =>
            Ok(await _service.ApplyAsync(dto));

        [HttpPut("{id}/status")]
        public async Task<ActionResult> SetStatus(int id, LeaveStatusDto dto)
        {
            var userId = Request.GetUserId();
            var success = await _service.SetStatusAsync(id, dto.Status, userId == 0 ? null : userId);
            return success ? Ok() : NotFound();
        }
    }
}
