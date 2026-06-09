using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;


[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class ExamResultController : ControllerBase
{
    private readonly ISMSExamResultService _service;

    public ExamResultController(ISMSExamResultService service)
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

    [HttpGet("byExam/{examId}")]
    public async Task<IActionResult> GetByExam(
        int examId,
        [FromQuery] int subjectId = 0,
        [FromQuery] int classId = 0,
        [FromQuery] int sectionId = 0)
    {
        var record = await _service.GetByExamAsync(examId, subjectId, classId, sectionId);
        return Ok(record);
    }

    [HttpGet("result-sheet")]
    public async Task<IActionResult> GetResultSheet(
        [FromQuery] int examId,
        [FromQuery] int classId,
        [FromQuery] int sectionId,
        [FromQuery] int prevExamId = 0)
    {
        var sheet = await _service.GetResultSheetAsync(examId, classId, sectionId, prevExamId);
        return Ok(sheet);
    }

    [HttpGet("result-card")]
    public async Task<IActionResult> GetResultCard(
        [FromQuery] int examId,
        [FromQuery] int classId,
        [FromQuery] int sectionId,
        [FromQuery] int prevExamId = 0,
        [FromQuery] int studentId = 0)
    {
        var card = await _service.GetResultCardAsync(examId, classId, sectionId, prevExamId, studentId);
        return Ok(card);
    }

    [HttpGet("position-report")]
    public async Task<IActionResult> GetPositionReport(
        [FromQuery] int examId,
        [FromQuery] int classId)
    {
        var rows = await _service.GetPositionReportAsync(examId, classId);
        return Ok(rows);
    }

    [HttpGet("percentage-report")]
    public async Task<IActionResult> GetPercentageReport(
        [FromQuery] int examId,
        [FromQuery] int classId)
    {
        var rows = await _service.GetPercentageReportAsync(examId, classId);
        return Ok(rows);
    }

    [HttpGet("executive-report")]
    public async Task<IActionResult> GetExecutiveReport(
        [FromQuery] int examId,
        [FromQuery] int classId,
        [FromQuery] int sectionId)
    {
        var report = await _service.GetExecutiveReportAsync(examId, classId, sectionId);
        return Ok(report);
    }

    [HttpGet("announcement-report")]
    public async Task<IActionResult> GetAnnouncementReport([FromQuery] int examId)
    {
        var report = await _service.GetAnnouncementReportAsync(examId);
        return Ok(report);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> FillCombo(
        [FromQuery] int examId = 0,
        [FromQuery] int studentId = 0,
        [FromQuery] int subjectId = 0,
        [FromQuery] int classId = 0,
        [FromQuery] int sectionId = 0)
    {
        var record = await _service.FillComboAsync(examId, studentId, subjectId, classId, sectionId);
        return Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var record = await _service.DeleteByIdAsync(id);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Save(SMSExamResultSaveDto dto)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveAsync(dto, userId, userIp);
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> SaveBulk(SMSExamResultBulkDto bulk)
    {
        int userId = Request.GetUserId();
        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.SaveBulkAsync(bulk, userId, userIp);
        return Ok(result);
    }
}
