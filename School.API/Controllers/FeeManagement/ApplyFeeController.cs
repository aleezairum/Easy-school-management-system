using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.FeeManagement;
using School.API.Extensions;
using School.API.Services.Interfaces.Accounts;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class ApplyFeeController : ControllerBase
{
    private readonly IApplyFeeService _service;

    public ApplyFeeController(IApplyFeeService service)
    {
        _service = service;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] ApplyFeePreviewRequestDto dto)
    {
        var result = await _service.GetPreviewAsync(dto);
        return Ok(result);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyFeeRequestDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await _service.ApplyFeeAsync(dto, userId, userIp);
        return Ok(result);
    }
    /// <param name="asOn">
    /// The day to judge arrears by — the voucher's own date. Fees are applied
    /// months in advance, and a row dated next month is not owed yet; each row
    /// comes back marked IsDue so the screen can add up the two separately.
    /// Null means today.
    /// </param>
    [HttpGet("Pendings")]
    public async Task<IActionResult> GetPendings(
        [FromQuery] int studentId,
        [FromQuery] DateTime? asOn)
    {
        var result = await _service.GetPendingsAsync(studentId, asOn);
        return Ok(result);
    }

    /// <summary>
    /// The Find Student dropdowns on the Receive Payment screen: the children
    /// of a class or section who actually owe something as at <paramref name="asOn"/>,
    /// with the amount, rather than every child on the roll.
    /// </summary>
    /// <param name="classId">0 or omitted for every class.</param>
    /// <param name="sectionId">0 or omitted for every section.</param>
    /// <param name="asOn">The voucher's own date. Null means today.</param>
    [HttpGet("PendingStudents")]
    public async Task<IActionResult> GetPendingStudents(
        [FromQuery] int classId,
        [FromQuery] int sectionId,
        [FromQuery] DateTime? asOn)
    {
        var result = await _service.GetPendingStudentsAsync(classId, sectionId, asOn);
        return Ok(result);
    }


    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? studentId,
        [FromQuery] int? sessionId,
        [FromQuery] int? sectionId)
    {
        var dto = new GetStudentFeeRequestDto
        {
            DateFrom = dateFrom ?? DateTime.Now.AddMonths(-1),
            DateTo = dateTo ?? DateTime.Now,
            StudentID = studentId,
            SessionID = sessionId,
            SectionID = sectionId
        };

        var result = await _service.GetListAsync(dto);
        return Ok(result);
    }

    [HttpGet("Report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? studentId,
        [FromQuery] int? sessionId,
        [FromQuery] string? sectionIds,
        [FromQuery] string? classIds,
        [FromQuery] string? feeTypeIds,
        [FromQuery] string? campusIds,
        [FromQuery] bool includePromised,
        [FromQuery] int reportType = 2)
    {
        var dto = new GetStudentFeeReportRequestDto
        {
            DateFrom   = dateFrom ?? DateTime.Now.AddMonths(-1),
            DateTo     = dateTo   ?? DateTime.Now,
            StudentID  = studentId,
            SessionID  = sessionId,
            SectionIDs = sectionIds,
            ClassIDs   = classIds,
            FeeTypeIDs = feeTypeIds,
            CampusIDs  = campusIds,
            IncludePromised = includePromised,
            ReportType = reportType
        };

        var result = await _service.GetReportAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = Request.GetUserId();
        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _service.DeleteAsync(id, userId, userIp);

        if (result.ReturnCode == 200)
            return Ok(result.ReturnMessage);
        else
            return StatusCode(500, result.ReturnMessage);
    }
}