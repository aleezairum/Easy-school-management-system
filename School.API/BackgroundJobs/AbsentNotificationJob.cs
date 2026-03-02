using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.BackgroundJobs
{
    public class AbsentNotificationJob : SmsJobBase
    {
        private DateTime _lastRunDate = DateTime.MinValue;

        public AbsentNotificationJob(IServiceScopeFactory scopeFactory, ILogger<AbsentNotificationJob> logger)
            : base(scopeFactory, logger, "AbsentNotification")
        {
        }

        protected override bool IsEnabled(IConfiguration config)
        {
            return config.GetValue<bool>("SmsJobs:AbsentNotification:Enabled");
        }

        protected override bool ShouldRunNow(IConfiguration config, DateTime now)
        {
            // Already ran today
            if (_lastRunDate.Date == now.Date)
                return false;

            var runHour = config.GetValue<int>("SmsJobs:AbsentNotification:RunAtHour");
            var runMinute = config.GetValue<int>("SmsJobs:AbsentNotification:RunAtMinute");

            return now.Hour == runHour && now.Minute >= runMinute && now.Minute < runMinute + 2;
        }

        protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken ct)
        {
            var smsService = services.GetRequiredService<ISmsService>();
            var logger = services.GetRequiredService<ILogger<AbsentNotificationJob>>();

            var absentStudents = await smsService.GetAbsentStudentsAsync(DateTime.Today);

            if (absentStudents.Count == 0)
            {
                logger.LogInformation("[AbsentNotification] No absent students found for today");
                return;
            }

            var dto = new SmsSendDto
            {
                Recipients = absentStudents,
                Message = "Dear Parent, your child was marked absent today. Please contact the school for details.",
                MessageType = SmsMessageType.Absent
            };

            var result = await smsService.SendBulkAsync(dto);
            logger.LogInformation("[AbsentNotification] Sent: {Sent}, Failed: {Failed}", result.TotalSent, result.TotalFailed);

            _lastRunDate = DateTime.Today;
        }
    }
}
