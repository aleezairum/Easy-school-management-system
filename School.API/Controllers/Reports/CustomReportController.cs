using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Reports;
using School.API.Services.Interfaces.Reports;

namespace School.API.Controllers.Reports
{
    /// <summary>
    /// Custom reports over students and employees — the caller chooses the
    /// columns and the filters.
    /// </summary>
    /// <remarks>
    /// The safety of this endpoint rests entirely on ReportFieldCatalog: column
    /// and operator keys are looked up there and the SQL comes from that file,
    /// values are always parameters. See CustomReportRepository.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomReportController : ControllerBase
    {
        private readonly ICustomReportService _service;

        public CustomReportController(ICustomReportService service)
        {
            _service = service;
        }

        /// <summary>Columns and operators the screen may offer for an entity.</summary>
        [HttpGet("schema")]
        public IActionResult GetSchema([FromQuery] string entity = "student")
        {
            try
            {
                return Ok(_service.GetSchema(entity));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run(CustomReportRequestDto request)
        {
            try
            {
                return Ok(await _service.RunAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
