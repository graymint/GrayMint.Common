using System.Collections.Concurrent;
using System.Diagnostics;
using GrayMint.Common.AspNetCore.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.Test;

[TestClass]
public class JobServiceTest
{
    [TestMethod]
    public async Task Runs_the_job_at_its_due_time_then_an_interval_after_each_run()
    {
        var runs = new JobRuns();
        await using var serviceProvider = CreateServiceProvider<QuickJob>(runs);
        using var jobService = CreateJobService(serviceProvider, NullLogger<QuickJob>.Instance,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero, Interval = TimeSpan.FromMilliseconds(200) });

        var stopwatch = Stopwatch.StartNew();
        await jobService.StartAsync(CancellationToken.None);
        await runs.ThirdStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await jobService.StopAsync(CancellationToken.None);

        // two intervals between the three runs: the wait is not skipped
        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(380), stopwatch.Elapsed);
    }

    [TestMethod]
    public async Task A_failed_run_is_logged_and_the_job_runs_again()
    {
        var runs = new JobRuns();
        var logger = new LastLogger<FailingJob>();
        await using var serviceProvider = CreateServiceProvider<FailingJob>(runs);
        using var jobService = CreateJobService(serviceProvider, logger,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero, Interval = TimeSpan.FromMilliseconds(50) });

        await jobService.StartAsync(CancellationToken.None);
        await runs.ThirdStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await jobService.StopAsync(CancellationToken.None);

        Assert.AreEqual(LogLevel.Error, logger.Level);
        Assert.AreEqual("Job failed. JobName: FailingJob", logger.Message);
    }

    [TestMethod]
    public async Task More_failures_in_a_row_than_MaxRetry_stop_the_job_with_an_error()
    {
        var runs = new JobRuns();
        var logger = new LastLogger<FailingJob>();
        await using var serviceProvider = CreateServiceProvider<FailingJob>(runs);
        using var jobService = CreateJobService(serviceProvider, logger,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero, Interval = TimeSpan.FromMilliseconds(50), MaxRetry = 2 });

        await jobService.StartAsync(CancellationToken.None);
        Assert.IsNotNull(jobService.ExecuteTask);
        await jobService.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10));
        await jobService.StopAsync(CancellationToken.None);

        Assert.AreEqual(3, runs.Count);
        Assert.AreEqual("Job failed too many times and stopped. JobName: FailingJob, FailedCount: 3", logger.Message);
    }

    [TestMethod]
    public async Task Stop_waits_for_the_run_in_progress_then_runs_the_job_on_shutdown()
    {
        var runs = new JobRuns();
        await using var serviceProvider = CreateServiceProvider<SlowToStopJob>(runs);
        using var jobService = CreateJobService(serviceProvider, NullLogger<SlowToStopJob>.Instance,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero, ExecuteOnShutdown = true });

        await jobService.StartAsync(CancellationToken.None);
        await runs.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await jobService.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new[] { "start 1", "end 1", "start 2", "end 2" }, runs.Events.ToArray());
    }

    [TestMethod]
    public async Task Stop_gives_up_with_a_warning_when_the_host_shutdown_time_is_over()
    {
        var runs = new JobRuns();
        var logger = new LastLogger<StuckJob>();
        await using var serviceProvider = CreateServiceProvider<StuckJob>(runs);
        using var jobService = CreateJobService(serviceProvider, logger,
            new GrayMintJobOptions { DueTime = TimeSpan.Zero });

        await jobService.StartAsync(CancellationToken.None);
        await runs.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await jobService.StopAsync(shutdownCts.Token).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(LogLevel.Warning, logger.Level);
        runs.Release.SetResult();
    }

    [TestMethod]
    public async Task A_zero_interval_or_a_negative_due_time_is_refused()
    {
        var runs = new JobRuns();
        await using var serviceProvider = CreateServiceProvider<QuickJob>(runs);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateJobService(serviceProvider,
            NullLogger<QuickJob>.Instance, new GrayMintJobOptions { Interval = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateJobService(serviceProvider,
            NullLogger<QuickJob>.Instance, new GrayMintJobOptions { DueTime = TimeSpan.FromSeconds(-1) }));
    }

    private static ServiceProvider CreateServiceProvider<TJob>(JobRuns runs) where TJob : class, IGrayMintJob
    {
        return new ServiceCollection()
            .AddSingleton(runs)
            .AddScoped<TJob>()
            .BuildServiceProvider();
    }

    private static GrayMinJobService<TJob> CreateJobService<TJob>(IServiceProvider serviceProvider,
        ILogger<TJob> logger, GrayMintJobOptions jobOptions) where TJob : IGrayMintJob
    {
        return new GrayMinJobService<TJob>(serviceProvider.GetRequiredService<IServiceScopeFactory>(), logger,
            jobOptions);
    }

    private sealed class JobRuns
    {
        public int Count;
        public ConcurrentQueue<string> Events { get; } = new();
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ThirdStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Start()
        {
            var run = Interlocked.Increment(ref Count);
            Events.Enqueue($"start {run}");
            if (run == 1) FirstStarted.TrySetResult();
            if (run == 3) ThirdStarted.TrySetResult();
            return run;
        }
    }

    private sealed class QuickJob(JobRuns runs) : IGrayMintJob
    {
        public ValueTask RunJob(CancellationToken cancellationToken)
        {
            runs.Start();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingJob(JobRuns runs) : IGrayMintJob
    {
        public ValueTask RunJob(CancellationToken cancellationToken)
        {
            runs.Start();
            throw new InvalidOperationException("The job failed.");
        }
    }

    // its first run lasts until the stop cancels it, and takes a while to end after that
    private sealed class SlowToStopJob(JobRuns runs) : IGrayMintJob
    {
        public async ValueTask RunJob(CancellationToken cancellationToken)
        {
            var run = runs.Start();
            if (run == 1)
            {
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
            runs.Start();
            await runs.Release.Task;
        }
    }

    private sealed class LastLogger<T> : ILogger<T>
    {
        public LogLevel? Level { get; private set; }
        public string? Message { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Message = formatter(state, exception);
        }
    }
}
