using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Services.Interfaces.Academic;


[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class SectionController : ControllerBase
{
    private readonly ISMSSectionService _service;

    public SectionController(ISMSSectionService service)
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
    public async Task<IActionResult>DeleteById(int id)
    {
        var record = await _service.DeleteByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }
    [HttpGet("byClass/{classId}")]
    public async Task<IActionResult> GetByClass(int classId)
    {
        var allSections = await _service.GetAllAsync();
        var filtered = allSections.Where(s => s.ClassID == classId).ToList();
        return Ok(filtered);
    }

    [HttpPost]
    public async Task<IActionResult> Save(SMSSectionSaveDto dto)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}
