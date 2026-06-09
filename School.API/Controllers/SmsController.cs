using Microsoft.AspNetCore.Mvc;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        // ── DB-BACKED PROGRESS — survives server restart ──────────────
        [HttpGet("progress/{jobId}")]
        public async Task<ActionResult<SmsQueueStatusDto>> GetProgress(string jobId)
        {
            var status = await _smsService.GetJobStatusAsync(jobId);
            if (status.Total == 0)
                return NotFound(new { message = "Job not found" });
            return Ok(status);
        }

        // Lightweight endpoint for a global queue widget.
        [HttpGet("queue-status")]
        public async Task<ActionResult> GetQueueStatus()
        {
            var pending = await _smsService.GetOverallPendingCountAsync();
            return Ok(new { pending });
        }

        // ── ENQUEUE BULK SEND ─────────────────────────────────────────
        // Saves all recipients to the SmsMessages queue with Status=Pending.
        // SmsQueueProcessor (BackgroundService) picks them up and sends
        // them with a throttle. Returns 202 with the jobId so the client
        // can poll /progress/{jobId}.
        [HttpPost("send")]
        public async Task<ActionResult> Send([FromBody] SmsSendDto dto)
        {
            if (dto.Recipients == null || dto.Recipients.Count == 0)
                return BadRequest(new { message = "No recipients specified" });

            bool hasGlobalMessage = !string.IsNullOrWhiteSpace(dto.Message);
            bool allHaveOwnMessages = dto.Recipients.All(r => !string.IsNullOrWhiteSpace(r.Message));

            if (!hasGlobalMessage && !allHaveOwnMessages)
                return BadRequest(new { message = "Message is required" });

            var status = await _smsService.EnqueueBulkAsync(dto);
            return Accepted(status);
        }

        // ── ENQUEUE FEE REMINDER ─────────────────────────────────────
        [HttpPost("fee-reminder")]
        public async Task<ActionResult> SendFeeReminder([FromBody] FeeSmsSendDto dto)
        {
            if (dto.DueDate == default)
                return BadRequest(new { message = "Due date is required" });

            var status = await _smsService.EnqueueFeeReminderAsync(dto);
            return Accepted(status);
        }

        // ── HISTORY ──────────────────────────────────────────────────
        [HttpGet("history")]
        public async Task<ActionResult> GetHistory([FromQuery] SmsHistoryFilterDto filter)
        {
            var messages = await _smsService.GetHistoryAsync(filter);
            var total = await _smsService.GetHistoryCountAsync(filter);
            return Ok(new
            {
                data = messages,
                total,
                page = filter.Page,
                pageSize = filter.PageSize
            });
        }

        // ── ABSENT STUDENTS ───────────────────────────────────────────
        [HttpGet("absent-students")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetAbsentStudents(
            [FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var students = await _smsService.GetAbsentStudentsAsync(targetDate);
            return Ok(students);
        }

        // ── LATE STUDENTS ─────────────────────────────────────────────
        [HttpGet("late-students")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetLateStudents(
            [FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var students = await _smsService.GetLateStudentsAsync(targetDate);
            return Ok(students);
        }

        // ── FEE DEFAULTERS (old endpoint — kept for compatibility) ─────
        [HttpGet("fee-defaulters")]
        public async Task<ActionResult<List<SmsRecipientDto>>> GetFeeDefaulters()
        {
            var students = await _smsService.GetFeeDefaultersAsync();
            return Ok(students);
        }
    }
}
