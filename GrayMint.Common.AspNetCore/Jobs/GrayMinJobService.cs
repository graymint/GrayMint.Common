using GrayMint.Common.Jobs;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T>(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<GrayMinJobService<T>> logger,
    GrayMintJobOptions jobOptions,
    JobRunner jobRunner)
    : IHostedService, IJob where T : IGrayMintJob
{
    private CancellationTokenSource? _cancellationTokenSource;
    public JobSection JobSection { get; } = new(jobOptions);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        jobRunner.Add(this);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        jobRunner.Remove(this);
        if (jobOptions.ExecuteOnShutdown)
        {
            try
            {
                await JobSection.Enter(RunJob, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not execute the job in the shutdown. JobName: {JobName}", JobSection.Name);
            }
        }

        if (_cancellationTokenSource != null)
            await _cancellationTokenSource.CancelAsync();
    }

    public async Task RunJob()
    {
        if (_cancellationTokenSource?.IsCancellationRequested == true)
            return;

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        await service.RunJob(_cancellationTokenSource?.Token ?? CancellationToken.None);
    }
}
