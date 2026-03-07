using Microsoft.AspNetCore.Mvc;
using School.API.Data.DBModels.Accounts;
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

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] int? classId,
        [FromQuery] int? sectionId,
        [FromQuery] string? name,
        [FromQuery] string? rollNo,
        [FromQuery] string? fatherName,
        [FromQuery] string? fatherCNIC)
    {
        var students = await _service.GetAllAsync();
        var filtered = students.AsEnumerable();

        if (classId.HasValue && classId.Value > 0)
            filtered = filtered.Where(s => s.ClassID == classId.Value);
        if (sectionId.HasValue && sectionId.Value > 0)
            filtered = filtered.Where(s => s.SectionID == sectionId.Value);
        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(s => s.Name != null && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(rollNo))
            filtered = filtered.Where(s => s.RollNo != null && s.RollNo.Contains(rollNo, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(fatherName))
            filtered = filtered.Where(s => s.FatherName != null && s.FatherName.Contains(fatherName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(fatherCNIC))
            filtered = filtered.Where(s => s.FatherCNIC != null && s.FatherCNIC.Contains(fatherCNIC, StringComparison.OrdinalIgnoreCase));

        return Ok(filtered.ToList());
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

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents([FromQuery] int classId, [FromQuery] int sectionId)
    {
        var result = await _service.GetStudentsForComboAsync(classId, sectionId);
        return Ok(result);
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
    [HttpPatch("status-change")]
    public async Task<IActionResult> StatusChange(ChangeStudentStatusRequest dto)
    {
        try
        {
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.StatusChangeAsync(dto.StudentIds, dto.StatusID, userId, userIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }
    
    [HttpPatch("section-change")]
    public async Task<IActionResult> SectionChange(ChangeStudentStatusRequest dto)
    {
        try
        {
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.SectionChangeAsync(dto.StudentIds, dto.StatusID, userId, userIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPatch("grade-change")]
    public async Task<IActionResult> GradeChange(ChangeStudentStatusRequest dto)
    {
        try
        {
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.GradeChangeAsync(dto.StudentIds, dto.StatusID, dto.Description, userId, userIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPatch("fee-change")]
    public async Task<IActionResult> FeeChange(ChangeStudentStatusRequest dto)
    {
        try
        {
            int userId = 1; // later from JWT
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _service.FeeChangeAsync(dto.StudentIds, dto.Fee, dto.Description, userId, userIp);
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
    
    [HttpPatch("{id}/avail-academy")]
    public async Task<IActionResult> AvailAcademy(int id, AvailAcademyRequest dto)
    {
        int userId = 1; // later from JWT
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _service.AvailAcademyAsync(id, dto.IsAvailAcademy, userId, userIp);
        if (result.ReturnCode < 0)
            return NotFound(result);
        return Ok(result);
    }
}
