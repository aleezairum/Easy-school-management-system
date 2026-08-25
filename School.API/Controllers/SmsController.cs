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
        private readonly IWhatsAppChatService _chatService;
        private readonly IConfiguration _configuration;
        private readonly IWahaSessionStatusService _wahaStatus;

        public SmsController(
            ISmsService smsService,
            IWhatsAppChatService chatService,
            IConfiguration configuration,
            IWahaSessionStatusService wahaStatus)
        {
            _smsService = smsService;
            _chatService = chatService;
            _configuration = configuration;
            _wahaStatus = wahaStatus;
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

        // WAHA connection badge on the Send SMS page — cached ~20s server-side
        // (see WahaSessionStatusService), so this is safe to poll frequently.
        [HttpGet("waha-status")]
        public async Task<ActionResult> GetWahaStatus([FromQuery] bool refresh = false)
        {
            var status = await _wahaStatus.GetStatusAsync(refresh);
            return Ok(new
            {
                connected = status.Connected,
                status = status.Status,
                error = status.Error,
                checkedAt = status.CheckedAtUtc
            });
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

        // ── ENQUEUE RESULTS AS TEXT (no PDF) ─────────────────────────
        [HttpPost("result")]
        public async Task<ActionResult> SendResultText([FromBody] ResultTextSendDto dto)
        {
            if (dto.ExamId <= 0)
                return BadRequest(new { message = "Exam is required" });

            if (dto.Scope == "student" && (dto.StudentId ?? 0) <= 0)
                return BadRequest(new { message = "Student is required for student scope" });

            var status = await _smsService.EnqueueResultTextAsync(dto);
            if (status.Total == 0)
                return BadRequest(new { message = "No students with a result and a valid WhatsApp number were found." });

            return Accepted(status);
        }

        // ── ENQUEUE RESULT-CARD PDFs (WhatsApp documents) ────────────
        [HttpPost("rcard")]
        public async Task<ActionResult> SendRcard([FromBody] RcardSendDto dto)
        {
            if (dto.ExamId <= 0)
                return BadRequest(new { message = "Exam is required" });

            if (dto.Scope == "student" && (dto.StudentId ?? 0) <= 0)
                return BadRequest(new { message = "Student is required for student scope" });

            var status = await _smsService.EnqueueRcardAsync(dto);
            if (status.Total == 0)
                return BadRequest(new { message = "No students with a result and a valid WhatsApp number were found." });

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

        // ── MESSAGES VIEWER ──────────────────────────────────────────
        // Paged WhatsApp message log: template used, parameters sent, the
        // complete rendered message and the delivery outcome.
        [HttpGet("messages")]
        public async Task<ActionResult<SmsMessagePagedDto>> GetMessages(
            [FromQuery] SmsMessageFilterDto filter)
        {
            var result = await _smsService.GetMessagesAsync(filter);
            return Ok(result);
        }

        // Single row — backs the detail modal.
        [HttpGet("messages/{vid:int}")]
        public async Task<ActionResult<SmsMessageViewDto>> GetMessage(int vid)
        {
            var message = await _smsService.GetMessageAsync(vid);
            if (message is null)
                return NotFound(new { message = "Message not found" });
            return Ok(message);
        }

        // ── META DELIVERY WEBHOOK ────────────────────────────────────
        // Meta calls GET once to verify the endpoint, then POSTs every
        // sent/delivered/read/failed update for messages we sent.
        // Configure the URL + VerifyToken in the Meta app's Webhooks section.
        [HttpGet("webhook")]
        public ActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            var expected = _configuration["MetaWhatsApp:VerifyToken"];

            if (mode == "subscribe" &&
                !string.IsNullOrEmpty(expected) &&
                verifyToken == expected)
            {
                // Meta expects the raw challenge echoed back as plain text.
                return Content(challenge ?? string.Empty, "text/plain");
            }

            return Forbid();
        }

        [HttpPost("webhook")]
        public async Task<ActionResult> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            // Meta sends one callback URL for everything, so the payload is
            // offered to both consumers: delivery statuses update the campaign
            // rows, and incoming messages/statuses feed the chat threads.
            var updated = await _smsService.ApplyDeliveryWebhookAsync(payload);
            var chat = await _chatService.IngestWebhookAsync(payload);

            // Always 200 — a non-2xx makes Meta retry and eventually disable
            // the subscription, even when the payload was simply not for us.
            return Ok(new { updated, chat });
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
