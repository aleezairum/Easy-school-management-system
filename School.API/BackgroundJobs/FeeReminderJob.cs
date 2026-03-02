using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.Services.Interfaces;

namespace School.API.BackgroundJobs
{
    public class FeeReminderJob : SmsJobBase
    {
        private DateTime _lastRunMonth = DateTime.MinValue;

        public FeeReminderJob(IServiceScopeFactory scopeFactory, ILogger<FeeReminderJob> logger)
            : base(scopeFactory, logger, "FeeReminder")
        {
        }

        protected override bool IsEnabled(IConfiguration config)
        {
            return config.GetValue<bool>("SmsJobs:FeeReminder:Enabled");
        }

        protected override bool ShouldRunNow(IConfiguration config, DateTime now)
        {
            // Already ran this month
            if (_lastRunMonth.Year == now.Year && _lastRunMonth.Month == now.Month)
                return false;

            var dayOfMonth = config.GetValue<int>("SmsJobs:FeeReminder:DayOfMonth");
            var runHour = config.GetValue<int>("SmsJobs:FeeReminder:RunAtHour");
            var runMinute = config.GetValue<int>("SmsJobs:FeeReminder:RunAtMinute");

            return now.Day == dayOfMonth && now.Hour == runHour && now.Minute >= runMinute && now.Minute < runMinute + 2;
        }

        protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken ct)
        {
            var smsService = services.GetRequiredService<ISmsService>();
            var logger = services.GetRequiredService<ILogger<FeeReminderJob>>();

            var defaulters = await smsService.GetFeeDefaultersAsync();

            if (defaulters.Count == 0)
            {
                logger.LogInformation("[FeeReminder] No fee defaulters found");
                return;
            }

            var dto = new SmsSendDto
            {
                Recipients = defaulters,
                Message = "Dear Parent, this is a reminder that your child's school fee is pending. Please submit the fee at your earliest convenience.",
                MessageType = SmsMessageType.Fee
            };

            var result = await smsService.SendBulkAsync(dto);
            logger.LogInformation("[FeeReminder] Sent: {Sent}, Failed: {Failed}", result.TotalSent, result.TotalFailed);

            _lastRunMonth = DateTime.Today;
        }
    }
}
