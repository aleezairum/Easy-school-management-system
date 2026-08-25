using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/reports")]
    public class HrReportsController : ControllerBase
    {
        private readonly IHrReportsRepository _reports;
        private readonly IPayrollService _payroll;

        public HrReportsController(IHrReportsRepository reports, IPayrollService payroll)
        {
            _reports = reports;
            _payroll = payroll;
        }

        [HttpGet("attendance")]
        public async Task<ActionResult> GetAttendance(
            [FromQuery] string type,
            [FromQuery] int? year, [FromQuery] int? month, [FromQuery] DateTime? date,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? designationId)
        {
            switch (type?.ToLowerInvariant())
            {
                case "daily":
                    var d = date ?? DateTime.Today;
                    return Ok(await _reports.GetDailyAsync(d, designationId));

                case "monthly":
                    var y = year ?? DateTime.Today.Year;
                    var m = month ?? DateTime.Today.Month;
                    return Ok(await _reports.GetMonthlySummaryAsync(y, m, designationId));

                case "late":
                    var lf = from ?? DateTime.Today.AddDays(-30);
                    var lt = to ?? DateTime.Today;
                    return Ok(await _reports.GetLateComersAsync(lf, lt, designationId));

                case "absent":
                    var af = from ?? DateTime.Today.AddDays(-30);
                    var at = to ?? DateTime.Today;
                    return Ok(await _reports.GetAbsenteesAsync(af, at, designationId));

                default:
                    return BadRequest(new { message = "type must be one of: daily, monthly, late, absent" });
            }
        }

        [HttpGet("salary-sheet")]
        public async Task<ActionResult> GetSalarySheet([FromQuery] int runId, [FromQuery] string? format)
        {
            var rows = await _payroll.GetSalarySheetAsync(runId);

            if (!string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
                return Ok(rows);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Salary Sheet");

            var headers = new[] { "Employee Code", "Name", "Designation", "Basic", "Gross Earning", "Total Deduction", "Net Pay" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Row(1).Style.Font.Bold = true;

            var r = 2;
            foreach (var row in rows)
            {
                sheet.Cell(r, 1).Value = row.EmployeeCode;
                sheet.Cell(r, 2).Value = row.EmployeeName;
                sheet.Cell(r, 3).Value = row.DesignationName;
                sheet.Cell(r, 4).Value = row.Basic;
                sheet.Cell(r, 5).Value = row.GrossEarning;
                sheet.Cell(r, 6).Value = row.TotalDeduction;
                sheet.Cell(r, 7).Value = row.NetPay;
                r++;
            }
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalarySheet-{runId}.xlsx");
        }
    }
}
