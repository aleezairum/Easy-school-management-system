// Controllers/ApplyFeeController.cs
using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.FeeManagement;
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
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await _service.ApplyFeeAsync(dto, userId, userIp);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = 1;   // TODO: JWT se lena
        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _service.DeleteAsync(id, userId, userIp);

        if (result.ReturnCode == 200)
            return Ok(result.ReturnMessage);
        else
            return StatusCode(500, result.ReturnMessage);
    }
}