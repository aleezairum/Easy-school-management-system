using School.API.Data.DBModels.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;

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
            _minDelayMs      = config.GetValue<int?>("Waha:Throttle:MinDelayMs") ?? 8000;
            _maxDelayMs      = config.GetValue<int?>("Waha:Throttle:MaxDelayMs") ?? 12000;
            _batchSize       = config.GetValue<int?>("Waha:Queue:BatchSize") ?? 20;
            _idleSleepMs     = config.GetValue<int?>("Waha:Queue:IdleSleepMs") ?? 5000;
            _enabled         = config.GetValue<bool?>("Waha:Queue:Enabled") ?? true;
            _cooldownAfter   = config.GetValue<int?>("Waha:Throttle:CooldownAfter") ?? 30;
            _cooldownMinMs   = config.GetValue<int?>("Waha:Throttle:CooldownMinMs") ?? 180_000;
            _cooldownMaxMs   = config.GetValue<int?>("Waha:Throttle:CooldownMaxMs") ?? 300_000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SmsQueueProcessor starting. Enabled={Enabled} Batch={Batch} Delay={Min}-{Max}ms Idle={Idle}ms CooldownAfter={CooldownAfter} CooldownPause={CooldownMin}-{CooldownMax}ms",
                _enabled, _batchSize, _minDelayMs, _maxDelayMs, _idleSleepMs,
                _cooldownAfter, _cooldownMinMs, _cooldownMaxMs);

            if (!_enabled)
            {
                _logger.LogWarning("SmsQueueProcessor is disabled (Waha:Queue:Enabled=false). Exiting.");
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

            var batch = await repo.GetPendingBatchAsync(_batchSize);
            if (batch.Count == 0) return false;

            _logger.LogDebug("Processing batch of {Count} pending SMS", batch.Count);

            for (int i = 0; i < batch.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var msg = batch[i];
                try
                {
                    bool ok = await sender.SendAsync(msg.RecipientPhone, msg.MessageText);
                    await repo.UpdateStatusAsync(
                        msg.VID,
                        ok ? SmsStatus.Sent : SmsStatus.Failed,
                        ok ? null : "Sender returned false");
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
