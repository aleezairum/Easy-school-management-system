using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Controllers.HR
{
    [ApiController]
    [Route("api/hr/reports")]
    public class HrReportsController : ControllerBase
    {
        private readonly IHrReportsRepository _reports;
        private readonly IHrReportsService _service;
        private readonly IPayrollService _payroll;

        public HrReportsController(IHrReportsRepository reports, IHrReportsService service, IPayrollService payroll)
        {
            _reports = reports;
            _service = service;
            _payroll = payroll;
        }

        /// <summary>
        /// The Staff Attendance Report. Every type takes a date range and the
        /// campus / designation / employee filters, and shows late, early and
        /// out-of-school minutes with what payroll deducts for them.
        /// </summary>
        /// <param name="type">daily, summary, late, early, outing or absent.</param>
        /// <remarks>
        /// The old single-date (date=) and month (year=, month=) forms are
        /// still read, so a saved link keeps working: "monthly" is the summary.
        /// </remarks>
        [HttpGet("attendance")]
        public async Task<ActionResult> GetAttendance(
            [FromQuery] string? type,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? campusId, [FromQuery] int? designationId, [FromQuery] int? employeeId,
            [FromQuery] int? year, [FromQuery] int? month, [FromQuery] DateTime? date)
        {
            type = (type ?? "daily").ToLowerInvariant();
            if (type == "monthly") type = "summary";

            if (from == null && to == null && date.HasValue) { from = date; to = date; }
            if (from == null && to == null && year.HasValue && month is >= 1 and <= 12)
            {
                from = new DateTime(year.Value, month.Value, 1);
                to = from.Value.AddMonths(1).AddDays(-1);
            }
            var f = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var t = (to ?? DateTime.Today).Date;
            if (t < f) return BadRequest(new { message = "From must be on or before To." });
            if ((t - f).TotalDays > 366) return BadRequest(new { message = "Run the report for a year at most." });

            var filter = new HrReportFilter { CampusId = campusId, DesignationId = designationId, EmployeeId = employeeId };

            return type switch
            {
                "summary" => Ok(await _service.GetAttendanceSummaryAsync(f, t, filter)),
                "daily" or "late" or "early" or "outing" or "absent" => Ok(await _service.GetAttendanceRowsAsync(type, f, t, filter)),
                _ => BadRequest(new { message = "type must be one of: daily, summary, late, early, outing, absent" })
            };
        }

        /// <summary>
        /// Staff attendance cards: each employee's every day in the range -
        /// marked or not, holidays and weekly offs included - with the day's
        /// hours, the minutes and their deductions, and the totals.
        /// </summary>
        [HttpGet("attendance-card")]
        public async Task<ActionResult<List<AttendanceCardDto>>> GetAttendanceCards(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? campusId, [FromQuery] int? designationId, [FromQuery] int? employeeId)
        {
            var f = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var t = (to ?? f.AddMonths(1).AddDays(-1)).Date;
            if (t < f) return BadRequest(new { message = "From must be on or before To." });
            if ((t - f).TotalDays > 62) return BadRequest(new { message = "An attendance card covers two months at most." });

            var filter = new HrReportFilter { CampusId = campusId, DesignationId = designationId, EmployeeId = employeeId };
            return Ok(await _service.GetAttendanceCardsAsync(f, t, filter));
        }

        /// <summary>
        /// The Staff Statement — the printed sheet listing every employee with
        /// qualification, appointment date and monthly salary.
        /// </summary>
        /// <param name="asOn">
        /// The "w.e.f." date on the sheet. Defaults to today. It decides which
        /// pay structure is quoted, so it is a real filter, not just a caption.
        /// </param>
        /// <param name="joinedFrom">Appointment date from (the sheet's date range).</param>
        /// <param name="joinedTo">Appointment date to.</param>
        [HttpGet("staff-statement")]
        public async Task<ActionResult> GetStaffStatement(
            [FromQuery] DateTime? asOn,
            [FromQuery] int? designationId,
            [FromQuery] bool includeInactive = false,
            [FromQuery] int? campusId = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] DateTime? joinedFrom = null,
            [FromQuery] DateTime? joinedTo = null)
        {
            var rows = await _reports.GetStaffStatementAsync(
                asOn ?? DateTime.Today, designationId, includeInactive, campusId, employeeId, joinedFrom, joinedTo);

            return Ok(rows);
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
