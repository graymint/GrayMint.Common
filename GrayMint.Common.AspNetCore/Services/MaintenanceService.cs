using GrayMint.Common.Jobs;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore.Services;

internal class MaintenanceService : IHostedService, IJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;
    private readonly JobRunner _jobRunner;
    private CancellationTokenSource _cancellationTokenSource = new();
    public JobSection JobSection { get; } = new();
    public List<Tuple<Type, JobSection>> SqlMaintenanceJobs { get; } = [];

    public MaintenanceService(
        ILogger<MaintenanceService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _jobRunner = new JobRunner(false)
        {
            Interval = TimeSpan.FromSeconds(60),
            Logger = logger
        };
        _jobRunner.Add(this);

    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _jobRunner.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _jobRunner.Stop();
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    public async Task RunJob()
    {
        foreach (var job in SqlMaintenanceJobs)
        {
            using var jobLock = job.Item2.Enter();
            if (jobLock.IsEntered)
                await RunSqlServerMaintenanceJob(job.Item1);
        }
    }

    private async Task RunSqlServerMaintenanceJob(Type dbContextType)
    {
        try
        {
            _logger.LogInformation("Starting a Sql Maintenance job... DbContext: {DbContext}", dbContextType.Name);

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
            dbContext.Database.SetCommandTimeout(TimeSpan.FromHours(48));
            await dbContext.Database.ExecuteSqlRawAsync(GrayMintResource.SqlMaintenance, _cancellationTokenSource.Token);
            _logger.LogInformation("Sql Maintenance job has been finished. DbContext: {DbContext}", dbContextType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not complete a Maintenance job. DbContext: {DbContext}", dbContextType.Name);
        }
    }
}
