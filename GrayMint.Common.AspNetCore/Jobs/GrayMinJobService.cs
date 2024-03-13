using GrayMint.Common.JobController;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T>(
    IServiceProvider serviceProvider, 
    GrayMintJobOptions jobOptions, 
    JobRunner jobRunner)
    : IHostedService, IJob where T : IGrayMintJob
{
    private CancellationTokenSource? _cancellationTokenSource;
    public JobSection JobSection { get; } = new (jobOptions);

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
            await JobSection.Enter(RunJob, true);

        if (_cancellationTokenSource!=null)
            await _cancellationTokenSource.CancelAsync();
    }

    public Task RunJob()
    {
        if (_cancellationTokenSource?.IsCancellationRequested == true)
            return Task.CompletedTask;

        var scope = serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        return service.RunJob(_cancellationTokenSource?.Token ?? CancellationToken.None);
    }
}
