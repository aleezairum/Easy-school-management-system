using Microsoft.AspNetCore.Mvc;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

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
        private readonly ISmsRoutingPolicy _routingPolicy;
        private readonly ILogger<SmsController> _logger;
        private readonly School.API.Services.Interfaces.Academic.IExamControlService _examControl;

        // Results go out on WhatsApp only once the office has finalised them.
        private const string NotFinalized =
            "This exam's result is not finalised yet. Finalise it on the Exams screen before sending it.";

        // Controllers are created per request, so this keeps the "app secret is
        // missing" warning to one line per process instead of one per webhook.
        private static bool _warnedAboutMissingAppSecret;

        public SmsController(
            ISmsService smsService,
            IWhatsAppChatService chatService,
            IConfiguration configuration,
            IWahaSessionStatusService wahaStatus,
            ISmsRoutingPolicy routingPolicy,
            ILogger<SmsController> logger,
            School.API.Services.Interfaces.Academic.IExamControlService examControl)
        {
            _examControl = examControl;
            _smsService = smsService;
            _chatService = chatService;
            _configuration = configuration;
            _wahaStatus = wahaStatus;
            _routingPolicy = routingPolicy;
            _logger = logger;
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
            var held = await _smsService.GetHeldCountAsync(null);
            return Ok(new { pending, held });
        }

        // ── HOLD / RESUME ────────────────────────────────────────────
        // Rows parked because WAHA (free) was disconnected. Nothing has been
        // spent on them and nothing is lost: the user either reconnects WAHA
        // and resumes free, or releases the batch through the paid gateway.

        [HttpGet("held")]
        public async Task<ActionResult> GetHeld([FromQuery] string? jobId)
        {
            var held = await _smsService.GetHeldCountAsync(jobId);
            var unsent = await _smsService.GetUnsentCountAsync(jobId);
            var status = await _routingPolicy.GetFreeStatusAsync();
            return Ok(new
            {
                held,
                unsent,
                jobId,
                freeAvailable = status.Connected,
                freeStatus = status.Status,
                gatewayUrl = _routingPolicy.FreeGatewayUrl
            });
        }

        // Resume on the free route. Refused while WAHA is still disconnected —
        // releasing the rows now would only have the queue park them again, so
        // the user is told to connect WAHA first (or choose the paid route).
        [HttpPost("resume-free")]
        public async Task<ActionResult> ResumeFree([FromBody] SmsResumeDto? dto)
        {
            // forceRefresh: the user has just reconnected WAHA and is asking us
            // to look again, so the ~20s status cache must not answer this.
            var status = await _routingPolicy.GetFreeStatusAsync(forceRefresh: true);
            if (!status.Connected)
            {
                return Conflict(new
                {
                    message = $"WhatsApp (free) is still disconnected — status {status.Status}. " +
                              "Connect WAHA and try again, or send this batch through the paid gateway.",
                    freeAvailable = false,
                    freeStatus = status.Status
                });
            }

            var resumed = await _smsService.ResumeHeldFreeAsync(dto?.JobId);
            return Ok(new { resumed, route = "free", freeAvailable = true, freeStatus = status.Status });
        }

        // Resume on the paid gateway. The rows are stamped ForcePaid so the
        // queue never routes them back to WAHA.
        [HttpPost("resume-paid")]
        public async Task<ActionResult> ResumePaid([FromBody] SmsResumeDto? dto)
        {
            var resumed = await _smsService.ResumeHeldPaidAsync(dto?.JobId);
            return Ok(new { resumed, route = "paid" });
        }

        // ── REJECT ───────────────────────────────────────────────────
        // Cancels every unsent message (Pending or Held) at once, so nothing
        // else goes out. Terminal — rejected rows are never picked up again,
        // but they stay in the log so it is clear what was dropped.
        [HttpPost("reject")]
        public async Task<ActionResult> Reject([FromBody] SmsResumeDto? dto)
        {
            var rejected = await _smsService.RejectUnsentAsync(dto?.JobId);
            return Ok(new { rejected });
        }

        // Backs the "reject N messages?" confirmation on the SMS page.
        [HttpGet("unsent")]
        public async Task<ActionResult> GetUnsent([FromQuery] string? jobId)
        {
            var unsent = await _smsService.GetUnsentCountAsync(jobId);
            return Ok(new { unsent, jobId });
        }

        // ── PREVIEW ("Show List") ────────────────────────────────────
        // Resolves recipients and composes messages exactly as the matching
        // send would, but queues nothing — so the user can check the count and
        // the content before committing.

        [HttpPost("preview/send")]
        public async Task<ActionResult<SmsPreviewDto>> PreviewSend([FromBody] SmsSendDto dto)
            => Ok(await _smsService.PreviewBulkAsync(dto));

        [HttpPost("preview/fee-reminder")]
        public async Task<ActionResult<SmsPreviewDto>> PreviewFeeReminder([FromBody] FeeSmsSendDto dto)
        {
            if (dto.DueDate == default)
                return BadRequest(new { message = "Due date is required" });

            return Ok(await _smsService.PreviewFeeReminderAsync(dto));
        }

        [HttpPost("preview/result")]
        public async Task<ActionResult<SmsPreviewDto>> PreviewResult([FromBody] ResultTextSendDto dto)
        {
            if (dto.ExamId <= 0)
                return BadRequest(new { message = "Exam is required" });

            if (!await _examControl.IsFinalizedAsync(dto.ExamId))
                return BadRequest(new { message = NotFinalized });

            return Ok(await _smsService.PreviewResultTextAsync(dto));
        }

        [HttpPost("preview/rcard")]
        public async Task<ActionResult<SmsPreviewDto>> PreviewRcard([FromBody] RcardSendDto dto)
        {
            if (dto.ExamId <= 0)
                return BadRequest(new { message = "Exam is required" });

            if (!await _examControl.IsFinalizedAsync(dto.ExamId))
                return BadRequest(new { message = NotFinalized });

            return Ok(await _smsService.PreviewRcardAsync(dto));
        }

        // WAHA connection badge on the Send SMS page — cached ~20s server-side
        // (see WahaSessionStatusService), so this is safe to poll frequently.
        // messageType (optional) also reports whether that type is configured to
        // go out free — the page only has to warn about a disconnected WAHA for
        // the tabs that actually use it.
        [HttpGet("waha-status")]
        public async Task<ActionResult> GetWahaStatus(
            [FromQuery] bool refresh = false,
            [FromQuery] SmsMessageType? messageType = null)
        {
            var status = await _wahaStatus.GetStatusAsync(refresh);
            return Ok(new
            {
                connected = status.Connected,
                status = status.Status,
                error = status.Error,
                checkedAt = status.CheckedAtUtc,
                routesFree = messageType.HasValue && _routingPolicy.PrefersFree(messageType),
                // Where to go to bring the session back up (scan the QR).
                gatewayUrl = _routingPolicy.FreeGatewayUrl
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

            if (!await _examControl.IsFinalizedAsync(dto.ExamId))
                return BadRequest(new { message = NotFinalized });

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

            if (!await _examControl.IsFinalizedAsync(dto.ExamId))
                return BadRequest(new { message = NotFinalized });

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
        // Meta calls these with no sign-in; the signature check below is their guard.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<ActionResult> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            if (!IsSignedByMeta(payload))
            {
                _logger.LogWarning(
                    "[WA] Rejected a webhook POST with a missing or invalid X-Hub-Signature-256");

                // 403, not the usual 200: this did not come from Meta, so there
                // is nothing to acknowledge and no subscription to protect.
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            // Meta sends one callback URL for everything, so the payload is
            // offered to both consumers: delivery statuses update the campaign
            // rows, and incoming messages/statuses feed the chat threads.
            var updated = await _smsService.ApplyDeliveryWebhookAsync(payload);
            var chat = await _chatService.IngestWebhookAsync(payload);

            // Always 200 — a non-2xx makes Meta retry and eventually disable
            // the subscription, even when the payload was simply not for us.
            return Ok(new { updated, chat });
        }

        /// <summary>
        /// Is this POST really from Meta?
        /// </summary>
        /// <remarks>
        /// The webhook has to be public and unauthenticated — Meta has no other
        /// way to reach it — so the signature is the only thing separating a
        /// real message from a forged one. Without this check anyone who learns
        /// the URL can write straight into a parent's chat thread.
        ///
        /// Meta signs the RAW request body with HMAC-SHA256 keyed by the app
        /// secret and sends it as "X-Hub-Signature-256: sha256=&lt;hex&gt;". The
        /// body must be hashed exactly as received; re-serialising the JSON
        /// changes the bytes and the signature will never match.
        ///
        /// When MetaWhatsApp:AppSecret is empty the check is SKIPPED and the
        /// payload accepted, so adding this cannot take a working webhook
        /// offline — but the hole stays open until the secret is set. Find it
        /// at App Dashboard → App settings → Basic → App Secret.
        /// </remarks>
        private bool IsSignedByMeta(string rawBody)
        {
            var appSecret = _configuration["MetaWhatsApp:AppSecret"];

            if (string.IsNullOrWhiteSpace(appSecret))
            {
                if (!_warnedAboutMissingAppSecret)
                {
                    _warnedAboutMissingAppSecret = true;
                    _logger.LogWarning(
                        "[WA] MetaWhatsApp:AppSecret is not set — webhook payloads are being " +
                        "accepted WITHOUT signature verification. Anyone who knows the callback " +
                        "URL can inject messages into chat threads. Set the app secret to close this.");
                }
                return true;
            }

            var header = Request.Headers["X-Hub-Signature-256"].ToString();
            const string prefix = "sha256=";

            if (string.IsNullOrWhiteSpace(header) ||
                !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var expected = Convert.ToHexString(
                HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes(appSecret),
                    Encoding.UTF8.GetBytes(rawBody)))
                .ToLowerInvariant();

            var supplied = header[prefix.Length..].Trim().ToLowerInvariant();

            // Fixed-time compare: ordinary string equality returns as soon as
            // two characters differ, which tells an attacker through timing how
            // much of a guessed signature was correct. Mismatched lengths give
            // false rather than throwing.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(supplied));
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
