using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.Test;

[TestClass]
public class LoggingTest : TestBase
{
    [TestMethod]
    public async Task ExceptionMiddleware_logs_a_named_client_error_as_information_and_any_other_as_error()
    {
        await AssertMiddleware(new KeyNotFoundException(), 404, LogLevel.Information);
        await AssertMiddleware(new UnauthorizedAccessException(), 403, LogLevel.Information);
        await AssertMiddleware(new NotSupportedException { Data = { ["HttpStatusCode"] = 400 } }, 400, LogLevel.Information);
        await AssertMiddleware(new ArgumentException("A bug."), 400, LogLevel.Error);
        await AssertMiddleware(new Exception { Data = { ["HttpStatusCode"] = 503 } }, 503, LogLevel.Error);
    }

    [TestMethod]
    public async Task ExceptionMiddleware_rethrows_once_the_response_has_started()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var logger = new CapturingLogger();
        var middleware = new GrayMintExceptionHandlerExtension.GrayMintExceptionMiddleware(
            _ => throw new InvalidOperationException("Late."),
            Options.Create(new GrayMintExceptionHandlerOptions()), logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));
        Assert.AreEqual("Late.", ex.Message);
        Assert.IsNull(logger.Level);
    }

    [TestMethod]
    public async Task JobService_logs_a_failed_job()
    {
        var logger = new CapturingLogger();
        await using var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(new CapturingLoggerProvider(logger)))
            .AddScoped<FailingJob>()
            .AddGrayMintJob<FailingJob>(new GrayMintJobOptions { ExecuteOnShutdown = true })
            .BuildServiceProvider();

        // the job runs once at the stop
        var jobService = serviceProvider.GetRequiredService<IHostedService>();
        await jobService.StartAsync(TestCt);
        await jobService.StopAsync(TestCt);

        Assert.AreEqual(LogLevel.Error, logger.Level);
        Assert.AreEqual($"{typeof(LoggingTest).FullName}.{nameof(FailingJob)}", logger.CategoryName);
    }

    // the status the middleware answers for the exception, and the level it logs it at
    private static async Task AssertMiddleware(Exception exception, int statusCode, LogLevel logLevel)
    {
        var context = new DefaultHttpContext();
        var logger = new CapturingLogger();
        var middleware = new GrayMintExceptionHandlerExtension.GrayMintExceptionMiddleware(
            _ => throw exception, Options.Create(new GrayMintExceptionHandlerOptions()), logger);

        await middleware.Invoke(context);
        Assert.AreEqual(statusCode, context.Response.StatusCode, exception.GetType().Name);
        Assert.AreEqual(logLevel, logger.Level, exception.GetType().Name);
    }

    private sealed class FailingJob : IGrayMintJob
    {
        public ValueTask RunJob(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The job failed.");
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class CapturingLoggerProvider(CapturingLogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CategoryLogger(logger, categoryName);
        public void Dispose() { }
    }

    private sealed class CategoryLogger(CapturingLogger logger, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            logger.CategoryName = categoryName;
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private sealed class CapturingLogger : ILogger<GrayMintExceptionHandlerExtension.GrayMintExceptionMiddleware>
    {
        public LogLevel? Level { get; private set; }
        public string? CategoryName { get; set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
        }
    }
}
