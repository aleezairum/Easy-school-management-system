using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Student;
using School.API.Extensions;
using School.API.Services.Interfaces.Student;

[ApiController]
[Route("api/[controller]")]
public class StdAttendenceController : ControllerBase
{
    private readonly IStdAttendenceService _service;

    public StdAttendenceController(IStdAttendenceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] StdAttendenceFilterDto filter)
    {
        return Ok(await _service.GetAllAsync(filter));
    }

    /// <summary>
    /// Monthly attendance register: classId with sectionId for one section, or
    /// without it for every section of the class. month is yyyy-MM.
    /// </summary>
    [HttpGet("register")]
    public async Task<IActionResult> Register([FromQuery] int classId, [FromQuery] int? sectionId, [FromQuery] string month)
    {
        if (classId <= 0) return BadRequest(new { message = "Choose a class." });
        if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var first))
            return BadRequest(new { message = "Choose a month." });
        return Ok(await _service.GetRegisterAsync(classId, sectionId is > 0 ? sectionId : null, first.Year, first.Month));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _service.GetByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var record = await _service.DeleteByIdAsync(id, userId, userIp);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpDelete("by-filter")]
    public async Task<IActionResult> DeleteByFilter([FromQuery] StdAttendenceFilterDto filter)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.DeleteByFilterAsync(filter, userId, userIp);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save(StdAttendenceSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}