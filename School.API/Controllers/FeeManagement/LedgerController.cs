using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.FeeManagement;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Controllers.FeeManagement
{
    // Statements of account for the fee module: one student, or one family.
    [ApiController]
    [Route("api/fee/ledger")]
    public class LedgerController : ControllerBase
    {
        private readonly ILedgerService _service;

        public LedgerController(ILedgerService service)
        {
            _service = service;
        }

        [HttpGet("student/{studentId:int}")]
        public async Task<ActionResult<LedgerResponseDto>> GetStudentLedger(
            int studentId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            if (studentId <= 0) return BadRequest(new { message = "A student must be selected." });
            return Ok(await _service.GetStudentLedgerAsync(studentId, dateFrom, dateTo));
        }

        [HttpGet("family/{parentId:int}")]
        public async Task<ActionResult<LedgerResponseDto>> GetFamilyLedger(
            int parentId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            if (parentId <= 0) return BadRequest(new { message = "A family must be selected." });
            return Ok(await _service.GetFamilyLedgerAsync(parentId, dateFrom, dateTo));
        }

        // Families have no screen of their own, so the report picker searches
        // here: father/guardian name, CNIC, mobile, or any child's name/roll.
        [HttpGet("families")]
        public async Task<ActionResult<List<FamilyLookupDto>>> GetFamilies(
            [FromQuery] string? search,
            [FromQuery] int top = 50)
        {
            return Ok(await _service.GetFamiliesAsync(search, top));
        }
    }
}
