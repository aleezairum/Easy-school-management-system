using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;


[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class ExamController : ControllerBase
{
    private readonly ISMSExamService _service;

    public ExamController(ISMSExamService service)
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

    [HttpGet("filter")]
    public async Task<IActionResult> FillCombo()
    {
        var record = await _service.FillComboAsync();
        return Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var record = await _service.DeleteByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Save(SMSExamSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}
