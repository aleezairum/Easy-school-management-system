using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeeVoucherController : ControllerBase
    {
        private readonly ISMSSectionService _service;

        public FeeVoucherController(ISMSSectionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FeeVoucherListDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FeeVoucherDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        //[HttpGet("challan/{challanVoucherId}")]
        //public async Task<ActionResult<IEnumerable<FeeVoucherListDto>>> GetByChallan(int challanVoucherId)
        //{
        //    var result = await _service.GetByChallanAsync(challanVoucherId);
        //    return Ok(result);
        //}

        //[HttpGet("daterange")]
        //public async Task<ActionResult<IEnumerable<FeeVoucherListDto>>> GetByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        //{
        //    var result = await _service.GetByDateRangeAsync(fromDate, toDate);
        //    return Ok(result);
        //}

        //[HttpPost]
        //public async Task<ActionResult<FeeVoucherDto>> Create(CreateFeeVoucherDto dto)
        //{
        //    var result = await _service.CreateAsync(dto);
        //    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        //}

        //[HttpPut("{id}")]
        //public async Task<ActionResult<FeeVoucherDto>> Update(int id, UpdateFeeVoucherDto dto)
        //{
        //    var result = await _service.UpdateAsync(id, dto);
        //    if (result == null)
        //        return NotFound();
        //    return Ok(result);
        //}

        //[HttpDelete("{id}")]
        //public async Task<ActionResult> Delete(int id)
        //{
        //    var success = await _service.DeleteAsync(id);
        //    if (!success)
        //        return NotFound();
        //    return NoContent();
        //}
    }
}
