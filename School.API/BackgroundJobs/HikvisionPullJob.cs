using School.API.Services.Interfaces.HR;

namespace School.API.BackgroundJobs
{
    /// <summary>
    /// Reads every active attendance machine's punches once a minute - the
    /// safety net under the machine's own push, so a punch made while the
    /// network was down still arrives. Does nothing while no machine is set up.
    /// Hikvision:PullEnabled = false stops it; Hikvision:PullIntervalSeconds sets the pace.
    /// </summary>
    public class HikvisionPullJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HikvisionPullJob> _logger;
        private readonly bool _enabled;
        private readonly TimeSpan _interval;

        public HikvisionPullJob(IServiceScopeFactory scopeFactory, ILogger<HikvisionPullJob> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _enabled = config.GetValue("Hikvision:PullEnabled", true);
            _interval = TimeSpan.FromSeconds(Math.Max(15, config.GetValue("Hikvision:PullIntervalSeconds", 60)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("[Hikvision] Pull job disabled (Hikvision:PullEnabled = false).");
                return;
            }

            // Let the application finish starting first.
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                var wait = _interval;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IHikvisionService>().PullAllAsync();
                }
                catch (Exception ex)
                {
                    // Most often the tables are not there yet (HikvisionAttendance.sql
                    // not run). Say so, and look again in ten minutes, not every minute.
                    _logger.LogWarning(ex, "[Hikvision] Pull job tick failed; trying again in 10 minutes.");
                    wait = TimeSpan.FromMinutes(10);
                }

                try { await Task.Delay(wait, stoppingToken); } catch (TaskCanceledException) { break; }
            }
        }
    }
}
