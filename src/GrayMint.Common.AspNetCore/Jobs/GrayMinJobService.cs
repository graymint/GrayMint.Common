namespace GrayMint.Common.AspNetCore.Jobs;

// each job runs on its own schedule, apart from the app's other jobs: the next run starts an interval after the
// last one ended, and the wait between runs holds a timer, not a thread
public class GrayMinJobService<T> : BackgroundService where T : IGrayMintJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<T> _logger;
    private readonly GrayMintJobOptions _jobOptions;
    private readonly string _jobName;

    public GrayMinJobService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<T> logger,
        GrayMintJobOptions jobOptions)
    {
        // a zero interval would run the job back to back
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(jobOptions.Interval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(jobOptions.DueTime ?? TimeSpan.Zero, TimeSpan.Zero);
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _jobOptions = jobOptions;
        _jobName = jobOptions.Name ?? typeof(T).Name;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(_jobOptions.DueTime ?? _jobOptions.Interval, stoppingToken);
        var failedCount = 0;
        while (true)
        {
            failedCount = await RunService(stoppingToken) ? 0 : failedCount + 1;
            if (failedCount > _jobOptions.MaxRetry && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError("Job failed too many times and stopped. JobName: {JobName}, FailedCount: {FailedCount}",
                    _jobName, failedCount);
                return;
            }

            await Task.Delay(_jobOptions.Interval, stoppingToken);
        }
    }

    // the host disposes the app's services after the stop, so the base stop cancels the run in progress and waits
    // for it, within the host's shutdown time
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_jobOptions.ExecuteOnShutdown && !cancellationToken.IsCancellationRequested)
            await RunService(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            _logger.LogWarning("Job did not end within the host's shutdown time. JobName: {JobName}", _jobName);
    }

    // false when the run failed, which is logged unless the stop has cut it
    private async Task<bool> RunService(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await service.RunJob(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
                _logger.LogError(ex, "Job failed. JobName: {JobName}", _jobName);
            return false;
        }
    }
}
