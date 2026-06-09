using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.FeeManagement;
using School.API.Extensions;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Controllers.FeeManagement
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly IPaymentMethodService _service;

        public PaymentMethodController(IPaymentMethodService service)
        {
            _service = service;
        }

        // GET api/paymentmethod
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load payment methods.", error = ex.Message });
            }
        }

        // GET api/paymentmethod/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var record = await _service.GetByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Payment method with ID {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load payment method.", error = ex.Message });
            }
        }

        // DELETE api/paymentmethod/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            try
            {
                var record = await _service.DeleteByIdAsync(id);
                if (record == null)
                    return NotFound(new { message = $"Payment method with ID {id} not found." });

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete payment method.", error = ex.Message });
            }
        }

        // POST api/paymentmethod  (VID = 0 → Insert, VID > 0 → Update)
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] PaymentMethodDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is empty." });

                if (string.IsNullOrWhiteSpace(dto.VName))
                    return BadRequest(new { message = "Name is required." });

                int userId = Request.GetUserId();
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

                var result = await _service.SaveAsync(dto, userId, userIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to save payment method.", error = ex.Message });
            }
        }
    }
}