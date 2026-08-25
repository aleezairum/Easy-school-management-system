using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/holidays")]
    public class HolidayController : ControllerBase
    {
        private readonly IHolidayService _service;

        public HolidayController(IHolidayService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<HolidayDto>>> GetAll([FromQuery] int? year) =>
            Ok(await _service.GetAllAsync(year));

        [HttpGet("{id}")]
        public async Task<ActionResult<HolidayDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<HolidayDto>> Create(SaveHolidayDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HolidayDto>> Update(int id, SaveHolidayDto dto)
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

        [HttpPost("copy-year")]
        public async Task<ActionResult> CopyYear(CopyHolidayYearDto dto)
        {
            var count = await _service.CopyYearAsync(dto);
            return Ok(new { copied = count });
        }
    }
}
