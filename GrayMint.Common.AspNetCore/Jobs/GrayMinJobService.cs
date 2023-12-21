using GrayMint.Common.JobController;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T>(IServiceProvider serviceProvider, JobConfig jobConfig, JobRunner jobRunner)
    : IHostedService, IJob where T : IGrayMintJob
{
    private CancellationTokenSource? _cancellationTokenSource;
    public JobSection JobSection { get; } = new (jobConfig);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        jobRunner.Add(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        jobRunner.Remove(this);
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
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
