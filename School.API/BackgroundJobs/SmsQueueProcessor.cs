using System.Globalization;
using System.Text.Json;
using School.API.Data.DBModels.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Implementations.Academic;
using School.API.Services.Interfaces;
using School.API.Services.Interfaces.Academic;

namespace School.API.BackgroundJobs
{
    // Drains the SmsMessages queue (Status=Pending) one row at a time with
    // a randomized delay between sends to avoid WhatsApp anti-spam bans.
    // Runs for the lifetime of the process. A single instance is created
    // by AddHostedService, so there's only one concurrent sender.
    public class SmsQueueProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SmsQueueProcessor> _logger;
        private readonly int _minDelayMs;
        private readonly int _maxDelayMs;
        private readonly int _batchSize;
        private readonly int _idleSleepMs;
        private readonly bool _enabled;

        // Cooldown: after every N messages, pause for a longer random period
        private readonly int _cooldownAfter;     // send this many, then pause
        private readonly int _cooldownMinMs;     // min pause duration
        private readonly int _cooldownMaxMs;     // max pause duration
        private int _sentSinceCooldown;          // counter resets after each cooldown

        public SmsQueueProcessor(
            IServiceScopeFactory scopeFactory,
            ILogger<SmsQueueProcessor> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _minDelayMs      = config.GetValue<int?>("Wasender:Throttle:MinDelayMs") ?? 8000;
            _maxDelayMs      = config.GetValue<int?>("Wasender:Throttle:MaxDelayMs") ?? 12000;
            _batchSize       = config.GetValue<int?>("Wasender:Queue:BatchSize") ?? 20;
            _idleSleepMs     = config.GetValue<int?>("Wasender:Queue:IdleSleepMs") ?? 5000;
            _enabled         = config.GetValue<bool?>("Wasender:Queue:Enabled") ?? true;
            _cooldownAfter   = config.GetValue<int?>("Wasender:Throttle:CooldownAfter") ?? 30;
            _cooldownMinMs   = config.GetValue<int?>("Wasender:Throttle:CooldownMinMs") ?? 180_000;
            _cooldownMaxMs   = config.GetValue<int?>("Wasender:Throttle:CooldownMaxMs") ?? 300_000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SmsQueueProcessor starting. Enabled={Enabled} Batch={Batch} Delay={Min}-{Max}ms Idle={Idle}ms CooldownAfter={CooldownAfter} CooldownPause={CooldownMin}-{CooldownMax}ms",
                _enabled, _batchSize, _minDelayMs, _maxDelayMs, _idleSleepMs,
                _cooldownAfter, _cooldownMinMs, _cooldownMaxMs);

            if (!_enabled)
            {
                _logger.LogWarning("SmsQueueProcessor is disabled (Wasender:Queue:Enabled=false). Exiting.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool processedAny = await ProcessBatchAsync(stoppingToken);
                    if (!processedAny)
                    {
                        // Queue empty — sleep briefly before checking again.
                        await SafeDelay(_idleSleepMs, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SmsQueueProcessor tick failed; backing off");
                    await SafeDelay(10_000, stoppingToken);
                }
            }

            _logger.LogInformation("SmsQueueProcessor stopped");
        }

        // Returns true if at least one message was processed in this tick.
        private async Task<bool> ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo   = scope.ServiceProvider.GetRequiredService<ISmsMessageRepository>();
            var sender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
            var policy = scope.ServiceProvider.GetRequiredService<ISmsRoutingPolicy>();

            var batch = await repo.GetPendingBatchAsync(_batchSize);
            if (batch.Count == 0) return false;

            _logger.LogDebug("Processing batch of {Count} pending SMS", batch.Count);

            for (int i = 0; i < batch.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var msg = batch[i];

                // Park before doing any work: a free-routed row must not be
                // rendered, let alone sent, while WAHA is down. Held rows cost
                // nothing and are released by the user from the SMS page.
                var route = await policy.DecideAsync(msg.MessageType, msg.ForcePaid);
                if (route == SmsRoute.Hold)
                {
                    var freeStatus = await policy.GetFreeStatusAsync();
                    await repo.HoldAsync(
                        msg.VID,
                        $"WhatsApp (free) is disconnected — status {freeStatus.Status}. Waiting to be resumed.");

                    _logger.LogInformation(
                        "[Queue] Held msg {VID} ({Type}) — WAHA status {Status}",
                        msg.VID, msg.MessageType, freeStatus.Status);

                    // No provider was contacted, so no throttle delay is owed.
                    continue;
                }

                try
                {
                    // Build the outgoing template request. Result cards attach a
                    // freshly rendered PDF and result texts are composed here, so
                    // both can also produce parameters/body not known at enqueue.
                    BuiltMessage built;
                    if (msg.MessageType == SmsMessageType.Rcard)
                        built = await BuildResultCardAsync(scope.ServiceProvider, msg);
                    else if (msg.MessageType == SmsMessageType.Result && msg.RefStudentId.HasValue)
                        built = await BuildResultTextAsync(scope.ServiceProvider, msg);
                    else
                        built = BuildPlain(msg);

                    if (built.Request is not null)
                        built.Request.ForcePaid = msg.ForcePaid;

                    var outcome = built.Error is null
                        ? await sender.SendTemplateAsync(msg.RecipientPhone, built.Request!)
                        : SmsSendOutcome.Fail(built.Error);

                    // WAHA dropped between the routing check above and the send.
                    // Park the row rather than marking it failed — it is still
                    // unsent and still free to send once WAHA is back.
                    if (outcome.ShouldHold)
                    {
                        await repo.HoldAsync(msg.VID, outcome.Error);
                        _logger.LogInformation(
                            "[Queue] Held msg {VID} mid-batch: {Reason}", msg.VID, outcome.Error);
                        continue;
                    }

                    await repo.UpdateSendResultAsync(
                        msg.VID,
                        outcome.Success ? SmsStatus.Sent : SmsStatus.Failed,
                        outcome,
                        sender.ProviderName,
                        built.FullMessage,
                        built.TemplateParamsJson);

                    // Mirror the send into the contact's chat thread so the
                    // WhatsApp view shows the full history, not just replies.
                    if (outcome.Success)
                    {
                        var chat = scope.ServiceProvider.GetRequiredService<IWhatsAppChatService>();
                        await chat.MirrorOutboundAsync(
                            msg.RecipientPhone,
                            built.FullMessage ?? msg.MessageText,
                            outcome.ProviderMessageId,
                            msg.TemplateName,
                            "accepted",
                            msg.VID);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, "Send failed for msg {VID} phone {Phone}",
                        msg.VID, msg.RecipientPhone);
                    await repo.UpdateStatusAsync(
                        msg.VID, SmsStatus.Failed, Truncate(ex.ToString(), 500));
                }

                _sentSinceCooldown++;

                // Cooldown pause: after every N messages, take a long break
                // to avoid WhatsApp anti-spam blocking.
                if (_cooldownAfter > 0 && _sentSinceCooldown >= _cooldownAfter)
                {
                    int cooldownMs = Random.Shared.Next(_cooldownMinMs, _cooldownMaxMs + 1);
                    _logger.LogInformation(
                        "[Throttle] Sent {Count} messages — cooldown pause for {Seconds}s",
                        _sentSinceCooldown, cooldownMs / 1000);
                    _sentSinceCooldown = 0;
                    await SafeDelay(cooldownMs, ct);
                }
                else
                {
                    // Randomized delay between every message
                    int delay = NextDelayMs();
                    if (delay > 0) await SafeDelay(delay, ct);
                }
            }

            return true;
        }

        // What one queued row turns into just before it is handed to the sender:
        // the outgoing request plus anything computed here that must be written
        // back to the row (rendered body, template parameters).
        private sealed class BuiltMessage
        {
            public SmsTemplateRequest? Request { get; init; }
            public string? FullMessage { get; init; }
            public string? TemplateParamsJson { get; init; }

            // Set when the row cannot be sent at all (missing data); the sender
            // is skipped and this becomes the row's error.
            public string? Error { get; init; }

            public static BuiltMessage Failed(string error) => new() { Error = error };
        }

        // Ordinary text/template row: everything was resolved at enqueue time.
        private static BuiltMessage BuildPlain(SmsMessage msg)
        {
            var body = string.IsNullOrWhiteSpace(msg.FullMessage) ? msg.MessageText : msg.FullMessage!;

            return new BuiltMessage
            {
                Request = new SmsTemplateRequest
                {
                    Name = msg.TemplateName ?? string.Empty,
                    Language = msg.TemplateLanguage ?? "en",
                    BodyParams = DeserializeParams(msg.TemplateParams),
                    RenderedText = body,
                    MessageType = msg.MessageType
                },
                FullMessage = body
            };
        }

        // Generates the student's result-card PDF (on demand, honoring throttle)
        // and attaches it as the template's document header, with the row's
        // caption as the body.
        private async Task<BuiltMessage> BuildResultCardAsync(IServiceProvider sp, SmsMessage msg)
        {
            if (msg.RefExamId is null || msg.RefStudentId is null)
                return BuiltMessage.Failed("Missing exam/student reference for result card");

            var examResultService = sp.GetRequiredService<ISMSExamResultService>();
            var pdfRenderer       = sp.GetRequiredService<IPdfRenderer>();

            var card = await examResultService.GetResultCardAsync(
                msg.RefExamId.Value,
                msg.RefClassId ?? 0,
                msg.RefSectionId ?? 0,
                0,
                msg.RefStudentId.Value);

            var student = card?.Students?.FirstOrDefault();
            if (student is null)
                return BuiltMessage.Failed("No result-card data for this student/exam");

            var html = ResultCardHtmlBuilder.Build(student, msg.RefExamName);
            var pdf  = await pdfRenderer.RenderHtmlToPdfAsync(html);

            var safeRoll = string.Concat((student.RollNo ?? msg.RefStudentId.ToString())
                .Where(c => char.IsLetterOrDigit(c)));
            var fileName = $"ResultCard_{safeRoll}.pdf";

            // result_card takes {{1}} Student Name, {{2}} Class. These are
            // stamped at enqueue time; rebuild them only for rows that predate
            // that (or were queued before a template was configured).
            var parameters = DeserializeParams(msg.TemplateParams);
            if (parameters.Count == 0)
                parameters = new List<string>
                {
                    student.StudentName ?? string.Empty,
                    ClassWithSection(student.ClassName, student.SectionName)
                };

            return new BuiltMessage
            {
                Request = new SmsTemplateRequest
                {
                    Name = msg.TemplateName ?? string.Empty,
                    Language = msg.TemplateLanguage ?? "en",
                    BodyParams = parameters,
                    RenderedText = msg.MessageText,
                    DocumentBytes = pdf,
                    DocumentFileName = fileName,
                    DocumentMimeType = "application/pdf",
                    MessageType = msg.MessageType
                },
                FullMessage = msg.MessageText,
                TemplateParamsJson = JsonSerializer.Serialize(parameters)
            };
        }

        // Composes the student's result summary as TEXT (at send time).
        // MessageText carries the optional prefix.
        private async Task<BuiltMessage> BuildResultTextAsync(IServiceProvider sp, SmsMessage msg)
        {
            if (msg.RefExamId is null || msg.RefStudentId is null)
                return BuiltMessage.Failed("Missing exam/student reference for result text");

            var examResultService = sp.GetRequiredService<ISMSExamResultService>();

            var card = await examResultService.GetResultCardAsync(
                msg.RefExamId.Value,
                msg.RefClassId ?? 0,
                msg.RefSectionId ?? 0,
                0,
                msg.RefStudentId.Value);

            var student = card?.Students?.FirstOrDefault();
            if (student is null)
                return BuiltMessage.Failed("No result data for this student/exam");

            var prefix = string.IsNullOrWhiteSpace(msg.MessageText) ? null : msg.MessageText;
            var text = ResultTextBuilder.Build(student, msg.RefExamName, prefix);

            // Marks/position only exist once the result is composed, so the
            // template parameters are produced here and written back to the row.
            var parameters = new List<string>
            {
                student.StudentName ?? string.Empty,
                ClassWithSection(student.ClassName, student.SectionName),
                msg.RefExamName ?? string.Empty,
                $"{Num(student.GrandObt)}/{Num(student.GrandTotal)}",
                Pct(student.CurrentPct),
                student.Position.HasValue ? student.Position.Value.ToString() : "-",
                student.GrandStatus ?? string.Empty
            };

            return new BuiltMessage
            {
                Request = new SmsTemplateRequest
                {
                    Name = msg.TemplateName ?? string.Empty,
                    Language = msg.TemplateLanguage ?? "en",
                    BodyParams = parameters,
                    RenderedText = text,
                    MessageType = msg.MessageType
                },
                FullMessage = text,
                TemplateParamsJson = JsonSerializer.Serialize(parameters)
            };
        }

        private static List<string> DeserializeParams(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        // A class is always shown with its section, e.g. "1 G2".
        private static string ClassWithSection(string? className, string? sectionName)
        {
            var parts = new[] { className, sectionName }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim());

            return string.Join(" ", parts);
        }

        private static string Num(decimal? v)
            => v.HasValue
                ? (v.Value == Math.Floor(v.Value)
                    ? ((long)v.Value).ToString(CultureInfo.InvariantCulture)
                    : v.Value.ToString("0.##", CultureInfo.InvariantCulture))
                : "-";

        private static string Pct(decimal? v)
            => v.HasValue ? v.Value.ToString("0.00", CultureInfo.InvariantCulture) + "%" : "-";

        private int NextDelayMs()
        {
            if (_maxDelayMs <= 0) return 0;
            int min = Math.Max(0, _minDelayMs);
            int max = Math.Max(min + 1, _maxDelayMs);
            return Random.Shared.Next(min, max + 1);
        }

        private static async Task SafeDelay(int ms, CancellationToken ct)
        {
            try { await Task.Delay(ms, ct); }
            catch (TaskCanceledException) { /* shutdown */ }
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
