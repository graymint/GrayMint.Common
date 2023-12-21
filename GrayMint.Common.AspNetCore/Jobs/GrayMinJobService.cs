using GrayMint.Common.JobController;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T>(IServiceProvider serviceProvider, JobConfig jobConfig)
    : IHostedService, IJob where T : IGrayMintJob
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<GrayMinJobService<T>> _logger = serviceProvider.GetRequiredService<ILogger<GrayMinJobService<T>>>() ;
    public JobSection JobSection { get; } = new (jobConfig);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (JobRunner.Default.Logger == NullLogger.Instance)
            JobRunner.Default.Logger = _logger;
                
        _cancellationTokenSource = new CancellationTokenSource();
        JobRunner.Default.Add(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        JobRunner.Default.Remove(this);
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
