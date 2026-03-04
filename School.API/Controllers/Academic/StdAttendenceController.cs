using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Student;
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _service.GetByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var record = await _service.DeleteByIdAsync(id, userId, userIp);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Save(StdAttendenceSaveDto dto)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}