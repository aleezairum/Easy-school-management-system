using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Accounts;
using School.API.Services.Interfaces.Accounts;

[ApiController]
[Route("api/[controller]")]
public class FeeTypeController : ControllerBase
{
    private readonly IFeeTypeService _service;

    public FeeTypeController(IFeeTypeService service)
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
    public async Task<IActionResult> Save(FeeTypeSaveDto dto)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}
