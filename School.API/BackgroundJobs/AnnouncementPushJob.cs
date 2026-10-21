using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.MobileApp;
using School.API.Services.Implementations.MobileApp;

namespace School.API.BackgroundJobs
{
    /// <summary>
    /// Sends each announcement to the mobile app as a push notification once,
    /// on the day it starts running (straight away when it is saved already
    /// running). AppPushLog remembers what has gone.
    ///
    /// An announcement that started more than two days before its push came due
    /// (the API was down, or Firebase was switched on later) is marked skipped,
    /// not pushed - the app still lists it, but nobody is woken for old news.
    ///
    /// Does nothing while Firebase:Enabled is false.
    /// </summary>
    public class AnnouncementPushJob : BackgroundService
    {
        private const int StaleDays = 2;
        private const int MaxAttempts = 5;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly FcmSender _fcm;
        private readonly AppPushSignal _signal;
        private readonly IConfiguration _config;
        private readonly ILogger<AnnouncementPushJob> _logger;
        private readonly Dictionary<int, int> _attempts = new();

        public AnnouncementPushJob(IServiceScopeFactory scopeFactory, FcmSender fcm, AppPushSignal signal,
            IConfiguration config, ILogger<AnnouncementPushJob> logger)
        {
            _scopeFactory = scopeFactory; _fcm = fcm; _signal = signal; _config = config; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_fcm.Enabled)
            {
                _logger.LogInformation("[Push] Announcement push disabled (Firebase:Enabled = false or no ServiceAccountFile).");
                return;
            }

            // Let the application finish starting - unless an announcement is saved meanwhile.
            try { await _signal.WaitAsync(TimeSpan.FromSeconds(20), stoppingToken); } catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                var wait = TimeSpan.FromMinutes(1);
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Most often AppPushLog is not there yet (MobileApp.sql not run).
                    _logger.LogWarning(ex, "[Push] Announcement push tick failed; trying again in 10 minutes.");
                    wait = TimeSpan.FromMinutes(10);
                }

                try { await _signal.WaitAsync(wait, stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }

        private async Task TickAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
            var today = DateTime.Today;

            var due = await db.SMSAnnouncements.AsNoTracking()
                .Where(a => a.IsActive && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)
                            && !db.AppPushLogs.Any(l => l.AnnouncementId == a.VID))
                .OrderBy(a => a.VID)
                .Select(a => new { a.VID, a.Title, a.Message, a.Audience, a.CampusID, a.StartDate, a.InsertedDate, HasImage = a.ImageData != null })
                .Take(20)
                .ToListAsync(ct);
            if (due.Count == 0) return;

            var prefix = AppTopics.Prefix(_config);
            var school = _config["App:SchoolCode"] ?? "";
            var publicApi = (_config["App:PublicApiUrl"] ?? "").TrimEnd('/');

            foreach (var a in due)
            {
                var dueSince = a.StartDate > a.InsertedDate.Date ? a.StartDate : a.InsertedDate.Date;
                AppPushLog log;
                if (dueSince < today.AddDays(-StaleDays))
                {
                    log = new AppPushLog { AnnouncementId = a.VID, Status = "skipped", Detail = $"Already running since {dueSince:dd-MMM-yyyy}" };
                }
                else
                {
                    var body = a.Message.Length > 180 ? a.Message[..177].TrimEnd() + "..." : a.Message;
                    var image = a.HasImage && publicApi.Length > 0 ? $"{publicApi}/api/announcement/{a.VID}/image" : null;
                    var r = await _fcm.SendToTopicsAsync(AppTopics.ForAnnouncement(prefix, a.Audience, a.CampusID), a.Title, body,
                        new Dictionary<string, string> { ["type"] = "announcement", ["id"] = a.VID.ToString(), ["school"] = school },
                        image, ct);

                    if (!r.Ok && r.Retry && (_attempts[a.VID] = _attempts.GetValueOrDefault(a.VID) + 1) < MaxAttempts)
                    {
                        _logger.LogWarning("[Push] Announcement {Id} not sent yet ({Detail}); trying again.", a.VID, r.Detail);
                        continue;
                    }
                    _attempts.Remove(a.VID);
                    log = new AppPushLog { AnnouncementId = a.VID, Status = r.Ok ? "sent" : "failed", Detail = r.Detail };
                    if (r.Ok) _logger.LogInformation("[Push] Announcement {Id} \"{Title}\" pushed.", a.VID, a.Title);
                    else _logger.LogWarning("[Push] Announcement {Id} push failed: {Detail}", a.VID, r.Detail);
                }

                db.AppPushLogs.Add(log);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
