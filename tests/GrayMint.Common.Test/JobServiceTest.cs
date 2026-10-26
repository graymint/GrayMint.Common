using System.Collections.Concurrent;
using GrayMint.Common.AspNetCore.Jobs;
using GrayMint.Common.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.Test;

[TestClass]
public class JobServiceTest
{
    [TestMethod]
    public async Task Stop_waits_for_the_run_in_progress_then_runs_the_job_on_shutdown()
    {
        var runs = new JobRuns();
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton(runs)
            .AddScoped<SlowToStopJob>()
            .BuildServiceProvider();
        var jobService = new GrayMinJobService<SlowToStopJob>(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<SlowToStopJob>.Instance,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero, ExecuteOnShutdown = true },
            new JobRunner(TimeSpan.FromMilliseconds(50)));

        await jobService.StartAsync(CancellationToken.None);
        await runs.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await jobService.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new[] { "start 1", "end 1", "start 2", "end 2" }, runs.Events.ToArray());
    }

    [TestMethod]
    public async Task Stop_gives_up_with_a_warning_when_the_host_shutdown_time_is_over()
    {
        var runs = new JobRuns();
        var logger = new LevelLogger<StuckJob>();
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton(runs)
            .AddScoped<StuckJob>()
            .BuildServiceProvider();
        var jobService = new GrayMinJobService<StuckJob>(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(), logger,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero }, new JobRunner(TimeSpan.FromMilliseconds(50)));

        await jobService.StartAsync(CancellationToken.None);
        await runs.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await jobService.StopAsync(shutdownCts.Token).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(LogLevel.Warning, logger.Level);
        runs.Release.SetResult();
    }

    private sealed class JobRuns
    {
        public int Count;
        public ConcurrentQueue<string> Events { get; } = new();
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    // its first run lasts until the stop cancels it, and takes a while to end after that
    private sealed class SlowToStopJob(JobRuns runs) : IGrayMintJob
    {
        public async ValueTask RunJob(CancellationToken cancellationToken)
        {
            var run = Interlocked.Increment(ref runs.Count);
            runs.Events.Enqueue($"start {run}");
            if (run == 1)
            {
                runs.FirstStarted.SetResult();
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                catch (OperationCanceledException) { await Task.Delay(200, CancellationToken.None); }
            }

            runs.Events.Enqueue($"end {run}");
        }
    }

    // ignores its cancellation
    private sealed class StuckJob(JobRuns runs) : IGrayMintJob
    {
        public async ValueTask RunJob(CancellationToken cancellationToken)
        {
            runs.FirstStarted.TrySetResult();
            await runs.Release.Task;
        }
    }

    private sealed class LevelLogger<T> : ILogger<T>
    {
        public LogLevel? Level { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
        }
    }
}
