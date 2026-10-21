using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;


[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class AcademicGradeController : ControllerBase
{
    private readonly IAcademicGradeService _service;

    public AcademicGradeController(IAcademicGradeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    /// <summary>
    /// Id and name only, in the shape the other dropdown endpoints use
    /// (Designation, HRGrade, Shift) so the screens can treat them alike.
    /// </summary>
    /// <remarks>
    /// Declared BEFORE the "{id}" route below, and it has to be: without it a
    /// request for /AcademicGrade/dropdown matched "{id}", failed to bind
    /// "dropdown" as an int and returned 400. The Employee form has been asking
    /// for this since it was written, so its Academic Grade list was always empty.
    /// </remarks>
    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown()
    {
        var grades = await _service.GetAllAsync();

        return Ok(grades
            .Where(g => g.IsActive != false)
            .Select(g => new { id = g.VID, name = g.VName }));
    }

    [HttpGet("{id:int}")]
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
    [HttpPost]
    public async Task<IActionResult> Save(AcademicGradeSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }
}
