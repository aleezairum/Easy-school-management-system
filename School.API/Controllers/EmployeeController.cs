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
        public async Task<ActionResult<List<EmployeeListDto>>> GetAll([FromQuery] int? campusId = null) => Ok(await _service.GetAllAsync(campusId));

        [HttpGet("dropdown")]
        public async Task<ActionResult<List<EmployeeDropdownDto>>> GetDropdown() => Ok(await _service.GetDropdownAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// The employee's picture, for an img tag: the photo saved on the form
        /// (a data: URL), or the face read from an attendance machine.
        /// </summary>
        [HttpGet("{id:int}/photo")]
        public async Task<ActionResult> GetPhoto(int id)
        {
            var photo = await _service.GetPhotoAsync(id);
            if (string.IsNullOrWhiteSpace(photo)) return NotFound();

            // An older row may hold a link rather than the picture itself.
            if (!photo.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return Redirect(photo);

            var comma = photo.IndexOf(',');
            if (comma < 0) return NotFound();
            var type = photo[5..comma].Replace(";base64", "", StringComparison.OrdinalIgnoreCase);
            try { return File(Convert.FromBase64String(photo[(comma + 1)..]), string.IsNullOrEmpty(type) ? "image/jpeg" : type); }
            catch (FormatException) { return NotFound(); }
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

        /// <summary>
        /// Sets the staff category on one or many employees, and nothing else.
        /// </summary>
        /// <remarks>
        /// Separate from the full save on purpose. The employee form requires a
        /// campus, a designation and a joining date, quite rightly — payroll and
        /// attendance need them. But a school whose older staff records were
        /// entered before those fields existed then cannot set a category on
        /// anybody without first inventing three unrelated pieces of data, and
        /// the office has seventy-odd people to put into categories.
        ///
        /// This touches one column. A null category clears it.
        /// </remarks>
        [HttpPut("category")]
        public async Task<ActionResult> SetCategory(SetStaffCategoryDto dto)
        {
            if (dto?.EmployeeIds == null || dto.EmployeeIds.Count == 0)
                return BadRequest(new { message = "Pick at least one member of staff." });

            var updated = await _service.SetStaffCategoryAsync(
                dto.EmployeeIds, dto.StaffCategoryId, Request.GetUserId(), CallerIp());

            return Ok(new { updated });
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
