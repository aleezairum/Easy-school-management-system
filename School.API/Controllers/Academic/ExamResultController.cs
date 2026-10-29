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
        [FromQuery] int classId,
        [FromQuery] int prevExamId = 0)
    {
        var rows = await _service.GetPositionReportAsync(examId, classId, prevExamId);
        return Ok(rows);
    }

    [HttpGet("percentage-report")]
    public async Task<IActionResult> GetPercentageReport(
        [FromQuery] int examId,
        [FromQuery] int classId,
        [FromQuery] int prevExamId = 0)
    {
        var rows = await _service.GetPercentageReportAsync(examId, classId, prevExamId);
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

    [HttpGet("subject-wise-report")]
    public async Task<IActionResult> GetSubjectWiseReport(
        [FromQuery] int examId,
        [FromQuery] string? classIds = null,
        [FromQuery] string? sectionIds = null)
    {
        var rows = await _service.GetSubjectWiseReportAsync(examId, classIds, sectionIds);
        return Ok(rows);
    }

    [HttpGet("teacher-wise-report")]
    public async Task<IActionResult> GetTeacherWiseReport(
        [FromQuery] int examId,
        [FromQuery] int teacherId = 0)
    {
        var rows = await _service.GetTeacherWiseReportAsync(examId, teacherId);
        return Ok(rows);
    }

    // Academy teachers: their Academy Subject Allocation, academy students' results only.
    [HttpGet("academy-teacher-wise-report")]
    public async Task<IActionResult> GetAcademyTeacherWiseReport(
        [FromQuery] int examId,
        [FromQuery] int teacherId = 0)
    {
        var rows = await _service.GetAcademyTeacherWiseReportAsync(examId, teacherId);
        return Ok(rows);
    }

    [HttpGet("teacher-comparison-report")]
    public async Task<IActionResult> GetTeacherComparisonReport(
        [FromQuery] int examId,
        [FromQuery] string? lClassIds = null,
        [FromQuery] string? lSectionIds = null,
        [FromQuery] string? rClassIds = null,
        [FromQuery] string? rSectionIds = null)
    {
        var rows = await _service.GetTeacherComparisonReportAsync(
            examId, lClassIds, lSectionIds, rClassIds, rSectionIds);
        return Ok(rows);
    }

    /// <param name="prevExamId">The exam for "Pass % Prev.": leave out for the previous term
    /// of the same kind (found from the paper dates), 0 for none.</param>
    [HttpGet("announcement-report")]
    public async Task<IActionResult> GetAnnouncementReport(
        [FromQuery] int examId,
        [FromQuery] string? classIds = null,
        [FromQuery] string? sectionIds = null,
        [FromQuery] int? prevExamId = null)
    {
        var report = await _service.GetAnnouncementReportAsync(examId, classIds, sectionIds, prevExamId);
        return Ok(report);
    }

    /// <summary>The exams of a session for the Comparison Result Sheet, first paper first; IsTerm marks the term exams.</summary>
    [HttpGet("comparison-exams")]
    public async Task<IActionResult> GetComparisonExams([FromQuery] int sessionId)
        => Ok(await _service.GetComparisonExamsAsync(sessionId));

    /// <summary>Class result % (students who passed every subject) per exam, class and side (B / G).</summary>
    /// <param name="examIds">Comma-separated exam ids.</param>
    [HttpGet("comparison")]
    public async Task<IActionResult> GetComparison([FromQuery] string examIds)
    {
        var ids = (examIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(s => int.TryParse(s, out var v) ? v : 0).Where(v => v > 0).Distinct().ToList();
        if (ids.Count == 0) return BadRequest(new { message = "Choose at least one exam." });
        return Ok(await _service.GetComparisonAsync(ids));
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

    // Marks Entry: a whole award list (Exam + Class + Section + Subject) moved to another date.
    [HttpPut("list/date")]
    public async Task<IActionResult> SetListDate(SMSExamResultListDateDto dto)
    {
        var (count, error) = await _service.SetListDateAsync(dto, Request.GetUserId());
        return error == null ? Ok(new { count }) : BadRequest(new { message = error });
    }

    // Marks Entry: a whole award list deleted (refused while the exam is finalised).
    [HttpDelete("list")]
    public async Task<IActionResult> DeleteList(
        [FromQuery] int examId, [FromQuery] int classId, [FromQuery] int sectionId, [FromQuery] int subjectId)
    {
        var (count, error) = await _service.DeleteListAsync(examId, classId, sectionId, subjectId);
        return error == null ? Ok(new { count }) : BadRequest(new { message = error });
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
