using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.UserManagement;
using School.API.Extensions;
using School.API.Services.Interfaces.UserManagement;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class SecUserController : ControllerBase
{
    private readonly ISecUserService _service;

    public SecUserController(ISecUserService service)
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
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var record = await _service.DeleteByIdAsync(id, userId, userIp);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Save(UserSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }

    /// <summary>Every user's roles, for the Role column on the list.</summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        return Ok(await _service.GetRolesAsync(null));
    }

    /// <summary>One user's roles, for the edit form.</summary>
    [HttpGet("{id:int}/roles")]
    public async Task<IActionResult> GetRoles(int id)
    {
        var rows = await _service.GetRolesAsync(id);
        return Ok(rows.FirstOrDefault() ?? new UserRolesDto { UserId = id });
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        if (result == null)
            return Unauthorized(new { message = "Invalid login or password" });
        return Ok(result);
    }
}
