using GrayMint.Common.Extensions;
using GrayMint.Common.Jobs;
using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T> : IHostedService where T : IGrayMintJob
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AsyncLock _runLock = new();
    private readonly Job _job;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<T> _logger;
    private readonly GrayMintJobOptions _jobOptions;

    public GrayMinJobService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<T> logger,
        GrayMintJobOptions jobOptions,
        JobRunner? jobRunner)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _jobOptions = jobOptions with { AutoStart = false };
        _job = new Job(RunJob, _jobOptions, jobRunner);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _job.Start();
        return Task.CompletedTask;
    }

    // the host disposes the app's services after the stop, so the stop cancels the run in progress and waits for it
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_job.IsStarted)
            _job.Stop();

        await _cancellationTokenSource.TryCancelAsync();
        try
        {
            using var runLock = await _runLock.LockAsync(cancellationToken);
            if (_jobOptions.ExecuteOnShutdown)
                await RunService(cancellationToken);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Job did not end within the host's shutdown time. JobName: {JobName}", typeof(T).Name);
        }
        catch
        {
            // RunService has logged it, and an exception would fail the host's stop
        }
    }

    private async ValueTask RunJob(CancellationToken cancellationToken)
    {
        using var runLock = await _runLock.LockAsync(cancellationToken);

        // the runner may still start a run it took just before the stop
        if (_cancellationTokenSource.IsCancellationRequested)
            return;

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        await RunService(linkedCts.Token);
    }

    private async Task RunService(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await service.RunJob(cancellationToken);
        }
        // the job runner has no logger of its own, so a failed job would pass unseen
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Job failed. JobName: {JobName}", typeof(T).Name);
            throw;
        }
    }
}