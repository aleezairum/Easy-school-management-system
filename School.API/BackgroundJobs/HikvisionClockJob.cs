using School.API.Services.Interfaces.HR;

namespace School.API.BackgroundJobs
{
    /// <summary>
    /// Keeps the attendance machines' clocks on the server's time. Every
    /// Hikvision:ClockCheckMinutes (default 60) it reads each active machine's
    /// clock and sets it to the server's when it is more than
    /// Hikvision:ClockMaxDriftSeconds (default 30) away - a machine running a few
    /// minutes fast gives every member of staff the wrong time in and out.
    /// Hikvision:ClockAutoCorrect = false stops it (Attendance Machines -> Machine
    /// Clocks still corrects by hand). Does nothing while no machine is set up.
    /// </summary>
    public class HikvisionClockJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HikvisionClockJob> _logger;
        private readonly bool _enabled;
        private readonly TimeSpan _interval;
        private readonly int _maxDrift;

        public HikvisionClockJob(IServiceScopeFactory scopeFactory, ILogger<HikvisionClockJob> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _enabled = config.GetValue("Hikvision:ClockAutoCorrect", true);
            _interval = TimeSpan.FromMinutes(Math.Max(5, config.GetValue("Hikvision:ClockCheckMinutes", 60)));
            _maxDrift = Math.Max(5, config.GetValue("Hikvision:ClockMaxDriftSeconds", 30));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("[Hikvision] Clock correction off (Hikvision:ClockAutoCorrect = false).");
                return;
            }

            // After the application and the pull job have started.
            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); } catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IHikvisionService>().CorrectClocksAsync(_maxDrift);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Hikvision] Clock check failed; trying again at the next check.");
                }

                try { await Task.Delay(_interval, stoppingToken); } catch (TaskCanceledException) { break; }
            }
        }
    }
}
