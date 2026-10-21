using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Extensions;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/payroll")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _service;

        public PayrollController(IPayrollService service)
        {
            _service = service;
        }

        [HttpGet("runs")]
        public async Task<ActionResult<List<PayrollRunDto>>> GetRuns() => Ok(await _service.GetRunsAsync());

        [HttpPost("runs/generate")]
        public async Task<ActionResult<PayrollRunResultDto>> Generate(GeneratePayrollRunDto dto)
        {
            var userId = Request.GetUserId();
            try
            {
                return Ok(await _service.GenerateAsync(dto, userId == 0 ? null : userId));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// The month's hand-entered amounts — incentive, fine, advance and the
        /// rest — for every member of staff. Zeroes where nothing is typed yet.
        /// </summary>
        /// <param name="designationIds">Comma separated. Empty means every designation.</param>
        [HttpGet("monthly-amounts")]
        public async Task<ActionResult<List<MonthlyAmountDto>>> GetMonthlyAmounts(
            [FromQuery] int year,
            [FromQuery] byte month,
            [FromQuery] int? campusId = null,
            [FromQuery] string? designationIds = null,
            [FromQuery] string? sortBy = null)
        {
            if (year < 2000 || month < 1 || month > 12)
                return BadRequest(new { message = "Pick a month." });

            var designations = string.IsNullOrWhiteSpace(designationIds)
                ? null
                : designationIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => int.TryParse(p, out var n) ? n : 0)
                    .Where(n => n > 0)
                    .Distinct()
                    .ToList();

            return Ok(await _service.GetMonthlyAmountsAsync(year, month, campusId, designations, sortBy));
        }

        /// <summary>
        /// Who qualifies for extra days in a month — no day off, and more
        /// working days than the threshold — with the reason for anyone who
        /// does not.
        /// </summary>
        /// <param name="minWorkingDays">Omit to use the saved setting (20 to start).</param>
        /// <param name="designationIds">Comma separated. Empty means every designation.</param>
        [HttpGet("extra-days")]
        public async Task<ActionResult<ExtraDaysSheetDto>> GetExtraDays(
            [FromQuery] int year,
            [FromQuery] byte month,
            [FromQuery] int? minWorkingDays = null,
            [FromQuery] int? campusId = null,
            [FromQuery] string? designationIds = null,
            [FromQuery] string? sortBy = null)
        {
            if (year < 2000 || month < 1 || month > 12)
                return BadRequest(new { message = "Pick a month." });
            if (minWorkingDays is < 0 or > 31)
                return BadRequest(new { message = "The working-days threshold must be between 0 and 31." });

            return Ok(await _service.GetExtraDaysAsync(
                year, month, minWorkingDays, campusId, ParseIds(designationIds), sortBy));
        }

        [HttpPut("extra-days")]
        public async Task<ActionResult<SaveExtraDaysResultDto>> SaveExtraDays(SaveExtraDaysDto dto)
        {
            if (dto == null || dto.Year < 2000 || dto.Month < 1 || dto.Month > 12)
                return BadRequest(new { message = "Pick a month." });

            try
            {
                var userId = Request.GetUserId();
                return Ok(await _service.SaveExtraDaysAsync(dto, userId == 0 ? null : userId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static List<int>? ParseIds(string? csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? null
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(p => int.TryParse(p, out var n) ? n : 0)
                     .Where(n => n > 0)
                     .Distinct()
                     .ToList();

        [HttpPut("monthly-amounts")]
        public async Task<ActionResult> SaveMonthlyAmounts(SaveMonthlyAmountsDto dto)
        {
            if (dto == null || dto.Year < 2000 || dto.Month < 1 || dto.Month > 12)
                return BadRequest(new { message = "Pick a month." });

            var userId = Request.GetUserId();
            var saved = await _service.SaveMonthlyAmountsAsync(dto, userId == 0 ? null : userId);
            return Ok(new { saved });
        }

        // ── Staff advances (by date) ───────────────────────────────────

        [HttpGet("advances")]
        public async Task<ActionResult<List<StaffAdvanceDto>>> GetAdvances(
            [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? employeeId = null, [FromQuery] int? campusId = null)
        {
            if (from == default || to == default || to < from)
                return BadRequest(new { message = "Choose the From and To dates." });
            return Ok(await _service.GetAdvancesAsync(from, to, employeeId, campusId));
        }

        [HttpPost("advances")]
        public Task<ActionResult> AddAdvance(SaveStaffAdvanceDto dto) => SaveAdvance(null, dto);

        [HttpPut("advances/{id:int}")]
        public Task<ActionResult> UpdateAdvance(int id, SaveStaffAdvanceDto dto) => SaveAdvance(id, dto);

        private async Task<ActionResult> SaveAdvance(int? id, SaveStaffAdvanceDto dto)
        {
            var userId = Request.GetUserId();
            try { return Ok(new { id = await _service.SaveAdvanceAsync(id, dto, userId == 0 ? null : userId) }); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("advances/{id:int}")]
        public async Task<ActionResult> DeleteAdvance(int id)
        {
            try { await _service.DeleteAdvanceAsync(id); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("runs/{id}")]
        public async Task<ActionResult<List<PayslipDto>>> GetRunPayslips(int id) => Ok(await _service.GetPayslipsAsync(id));

        [HttpPut("runs/{id}/lock")]
        public async Task<ActionResult> LockRun(int id)
        {
            var userId = Request.GetUserId();
            var success = await _service.LockRunAsync(id, userId == 0 ? null : userId);
            return success ? Ok() : NotFound();
        }

        // A Draft run only - to switch a month between all campuses and per campus.
        [HttpDelete("runs/{id}")]
        public async Task<ActionResult> DeleteRun(int id)
        {
            try
            {
                return await _service.DeleteRunAsync(id) ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("runs/{id}/pay")]
        public async Task<ActionResult> PayRun(int id, PayRunDto dto)
        {
            var success = await _service.PayRunAsync(id, dto);
            return success ? Ok() : NotFound();
        }

        /// <summary>
        /// Payslips of every month the range touches - the Salary Slips
        /// screen, which prints them. Filtered by the employee's campus,
        /// designation, or one employee.
        /// </summary>
        [HttpGet("payslips")]
        public async Task<ActionResult<List<PayslipDto>>> GetPayslips(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? campusId, [FromQuery] int? designationId, [FromQuery] int? employeeId)
        {
            var lastMonth = DateTime.Today.AddMonths(-1);
            var f = (from ?? new DateTime(lastMonth.Year, lastMonth.Month, 1)).Date;
            var t = (to ?? f.AddMonths(1).AddDays(-1)).Date;
            if (t < f) return BadRequest(new { message = "From must be on or before To." });

            return Ok(await _service.GetPayslipsForPeriodAsync(f, t, campusId, designationId, employeeId));
        }

        [HttpGet("payslip/{id}")]
        public async Task<ActionResult<PayslipDto>> GetPayslip(int id)
        {
            var result = await _service.GetPayslipAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("payslip/{id}/pay")]
        public async Task<ActionResult> PayEmployee(int id, PayEmployeeDto dto)
        {
            var success = await _service.PayEmployeeAsync(id, dto);
            return success ? Ok() : NotFound();
        }
    }
}
