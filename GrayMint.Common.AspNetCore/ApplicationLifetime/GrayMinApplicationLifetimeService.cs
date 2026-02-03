namespace GrayMint.Common.AspNetCore.ApplicationLifetime;

public class GrayMinApplicationLifetimeService<T>(
    IServiceScopeFactory scopeFactory,
    ILogger<GrayMinApplicationLifetimeService<T>> logger)
    : IHostedService where T : IGrayMintApplicationLifetime
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        await service.ApplicationStarted(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await service.ApplicationStopping(cancellationToken);

        }
        catch (Exception ex)
        {
            try { logger.LogError(ex, "Error during stopping."); }
            catch { Console.WriteLine(ex); }
        }
    }
}