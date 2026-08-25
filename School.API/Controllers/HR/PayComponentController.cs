using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/pay-components")]
    public class PayComponentController : ControllerBase
    {
        private readonly IPayComponentService _service;

        public PayComponentController(IPayComponentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PayComponentDto>>> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<PayComponentDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<PayComponentDto>> Create(SavePayComponentDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PayComponentDto>> Update(int id, SavePayComponentDto dto)
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

        [HttpPost("preview")]
        public async Task<ActionResult<PayComponentPreviewResultDto>> Preview(PayComponentPreviewRequestDto dto)
        {
            var result = await _service.PreviewAsync(dto);
            return result == null ? NotFound() : Ok(result);
        }
    }
}
