using Microsoft.AspNetCore.Mvc;
using School.API.DTOs.Academic;
using School.API.Extensions;
using School.API.Services.Interfaces.Academic;

namespace School.API.Controllers.Academic
{
    /// <summary>
    /// Finalising an exam's result (only finalised exams go to WhatsApp) and
    /// the students left out of the teachers' result.
    /// </summary>
    [ApiController]
    [Route("api/exam-control")]
    public class ExamControlController : ControllerBase
    {
        private readonly IExamControlService _service;

        public ExamControlController(IExamControlService service)
        {
            _service = service;
        }

        private int? UserId => Request.GetUserId() is var u && u > 0 ? u : null;

        [HttpGet("finalized")]
        public async Task<ActionResult<List<ExamFinalizeDto>>> GetFinalized() => Ok(await _service.GetFinalizedAsync());

        [HttpPost("{examId:int}/finalize")]
        public async Task<ActionResult> Finalize(int examId)
        {
            await _service.FinalizeAsync(examId, UserId);
            return Ok();
        }

        [HttpDelete("{examId:int}/finalize")]
        public async Task<ActionResult> Unfinalize(int examId)
        {
            await _service.UnfinalizeAsync(examId);
            return Ok();
        }

        /// <param name="classId">Leave out to list only the students already excluded.</param>
        [HttpGet("{examId:int}/exclude")]
        public async Task<ActionResult<List<ExamExcludeRowDto>>> GetExcludeList(int examId, [FromQuery] int? classId, [FromQuery] int? sectionId) =>
            Ok(await _service.GetExcludeListAsync(examId, classId, sectionId));

        [HttpPost("exclude")]
        public async Task<ActionResult> SaveExcludeList(SaveExamExcludeDto dto)
        {
            try { return Ok(new { excluded = await _service.SaveExcludeListAsync(dto, UserId) }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
