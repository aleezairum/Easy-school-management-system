using School.API.Services.Interfaces.Settings;

namespace School.API.BackgroundJobs
{
    // Polls every 60 seconds and runs the configured DB backup (dbo.SpGet_DBBackup)
    // whenever the current time matches one of the up-to-3 configured daily run
    // times (BackupJobSetting.RunTimesCsv, e.g. "06:00,14:00,22:00"). Settings are
    // read from the database each tick, so changes made on the Backup settings
    // page (School.Web: /Settings/Backup) take effect without restarting the API.
    //
    // Not built on SmsJobBase: that base's enable/schedule checks only receive
    // IConfiguration, but this job's schedule lives in the database, not
    // appsettings.json, so it needs its own small polling loop.
    public class DbBackupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DbBackupJob> _logger;

        // Tracks the last calendar date each configured "HH:mm" slot ran on,
        // so a slot fires at most once per day even though we poll every minute.
        private readonly Dictionary<string, DateTime> _lastRunDatePerSlot = new();

        public DbBackupJob(IServiceScopeFactory scopeFactory, ILogger<DbBackupJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DbBackupJob] Background job started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IBackupJobSettingService>();
                    var settings = await service.GetAsync();

                    if (settings is { IsEnabled: true })
                    {
                        var now = DateTime.Now;
                        foreach (var slot in ParseRunTimes(settings.RunTimesCsv))
                        {
                            var key = $"{slot.Hours:D2}:{slot.Minutes:D2}";
                            var isInWindow = now.Hour == slot.Hours && now.Minute >= slot.Minutes && now.Minute < slot.Minutes + 2;
                            var alreadyRanToday = _lastRunDatePerSlot.TryGetValue(key, out var lastDate) && lastDate.Date == now.Date;

                            if (isInWindow && !alreadyRanToday)
                            {
                                _logger.LogInformation("[DbBackupJob] Running scheduled backup for slot {Slot}", key);
                                var result = await service.RunBackupNowAsync(stoppingToken);
                                _lastRunDatePerSlot[key] = now;
                                _logger.LogInformation(
                                    "[DbBackupJob] Slot {Slot} finished — Success={Success}: {Message}",
                                    key, result.Success, result.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DbBackupJob] Error during execution");
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private static IEnumerable<TimeSpan> ParseRunTimes(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) yield break;

            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (TimeSpan.TryParse(part, out var t))
                    yield return t;
            }
        }
    }
}
