using GrayMint.Common.Jobs;
using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.Jobs;

public class GrayMinJobService<T> : IHostedService where T : IGrayMintJob
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Job _job;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly GrayMintJobOptions _jobOptions;

    public GrayMinJobService(
        IServiceScopeFactory serviceScopeFactory,
        GrayMintJobOptions jobOptions,
        JobRunner? jobRunner)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _jobOptions = jobOptions;
        _job = new Job(RunJob, _jobOptions, jobRunner);
        _job.Stop();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _job.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_jobOptions.ExecuteOnShutdown)
        {
            try
            {
                if (!_job.IsStarted)
                    await _job.RunNow(cancellationToken);
            }
            catch 
            {
                // no error on shutdown. RunNow already logs errors.
            }
        }

        _job.Stop();
        if (_cancellationTokenSource != null)
            await _cancellationTokenSource.TryCancelAsync();
    }

    private async ValueTask RunJob(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource?.IsCancellationRequested == true)
            return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource?.Token ?? CancellationToken.None);
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        await service.RunJob(linkedCts.Token);
    }
}
