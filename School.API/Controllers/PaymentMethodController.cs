using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly ISMSSectionService _service;

        public PaymentMethodController(ISMSSectionService service)
        {
            _service = service;
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<PaymentMethodListDto>>> GetAll()
        //{
        //    var result = await _service.GetAllAsync();
        //    return Ok(result);
        //}

        //[HttpGet("{id}")]
        //public async Task<ActionResult<PaymentMethodDto>> GetById(int id)
        //{
        //    var result = await _service.GetByIdAsync(id);
        //    if (result == null)
        //        return NotFound();
        //    return Ok(result);
        //}

        //[HttpGet("dropdown")]
        //public async Task<ActionResult<IEnumerable<PaymentMethodDropdownDto>>> GetDropdown()
        //{
        //    var result = await _service.GetDropdownAsync();
        //    return Ok(result);
        //}

        //[HttpPost]
        //public async Task<ActionResult<PaymentMethodDto>> Create(CreatePaymentMethodDto dto)
        //{
        //    var result = await _service.CreateAsync(dto);
        //    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        //}

        //[HttpPut("{id}")]
        //public async Task<ActionResult<PaymentMethodDto>> Update(int id, UpdatePaymentMethodDto dto)
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
