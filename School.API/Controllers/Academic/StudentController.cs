using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Services.Interfaces.Academic;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _service;

    public StudentController(IStudentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
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
        var record = await _service.DeleteByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Save(StudentSaveDto dto)
    {
        try
        {
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.SaveAsync(dto, userId, userIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, StudentSaveDto dto)
    {
        try
        {
            dto.VID = id;
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.SaveAsync(dto, userId, userIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.ToggleStatusAsync(id, userId, userIp);
        if (result.ReturnCode < 0)
            return NotFound(result);
        return Ok(result);
    }
}
