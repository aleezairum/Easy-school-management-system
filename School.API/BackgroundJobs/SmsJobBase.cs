namespace School.API.BackgroundJobs
{
    public abstract class SmsJobBase : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly string _jobName;

        protected SmsJobBase(IServiceScopeFactory scopeFactory, ILogger logger, string jobName)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jobName = jobName;
        }

        protected abstract bool IsEnabled(IConfiguration config);
        protected abstract bool ShouldRunNow(IConfiguration config, DateTime now);
        protected abstract Task ExecuteJobAsync(IServiceProvider services, CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[{Job}] Background job started", _jobName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    if (!IsEnabled(config))
                    {
                        _logger.LogDebug("[{Job}] Disabled — skipping", _jobName);
                    }
                    else if (ShouldRunNow(config, DateTime.Now))
                    {
                        _logger.LogInformation("[{Job}] Running scheduled task", _jobName);
                        await ExecuteJobAsync(scope.ServiceProvider, stoppingToken);
                        _logger.LogInformation("[{Job}] Completed", _jobName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Job}] Error during execution", _jobName);
                }

                // Check every 60 seconds
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
