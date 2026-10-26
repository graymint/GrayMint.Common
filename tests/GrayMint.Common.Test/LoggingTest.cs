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
public class LoggingTest
{
    [TestMethod]
    public async Task ExceptionMiddleware_logs_a_named_client_error_as_information_and_any_other_as_error()
    {
        Assert.AreEqual((404, LogLevel.Information), await InvokeMiddleware(new KeyNotFoundException()));
        Assert.AreEqual((403, LogLevel.Information), await InvokeMiddleware(new UnauthorizedAccessException()));
        Assert.AreEqual((400, LogLevel.Information),
            await InvokeMiddleware(new NotSupportedException { Data = { ["HttpStatusCode"] = 400 } }));
        Assert.AreEqual((400, LogLevel.Error), await InvokeMiddleware(new ArgumentException("A bug.")));
        Assert.AreEqual((503, LogLevel.Error),
            await InvokeMiddleware(new Exception { Data = { ["HttpStatusCode"] = 503 } }));
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
        await jobService.StartAsync(CancellationToken.None);
        await jobService.StopAsync(CancellationToken.None);

        Assert.AreEqual(LogLevel.Error, logger.Level);
        Assert.AreEqual($"{typeof(LoggingTest).FullName}.{nameof(FailingJob)}", logger.CategoryName);
    }

    private static async Task<(int, LogLevel?)> InvokeMiddleware(Exception exception)
    {
        var context = new DefaultHttpContext();
        var logger = new CapturingLogger();
        var middleware = new GrayMintExceptionHandlerExtension.GrayMintExceptionMiddleware(
            _ => throw exception, Options.Create(new GrayMintExceptionHandlerOptions()), logger);
        await middleware.Invoke(context);
        return (context.Response.StatusCode, logger.Level);
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
