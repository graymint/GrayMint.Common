﻿namespace GrayMint.Common.AspNetCore.ApplicationLifetime;

public class GrayMinApplicationLifetimeService<T>(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<GrayMinApplicationLifetimeService<T>> logger,
    IHostApplicationLifetime hostApplicationLifetime)
    : IHostedService where T : IGrayMintApplicationLifetime
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(() =>
        {
            using var scope = serviceScopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            // ReSharper disable once MethodSupportsCancellation
            service.ApplicationStarted().Wait();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {

        hostApplicationLifetime.ApplicationStopping.Register(() =>
        {
            using var scope = serviceScopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            
            // ReSharper disable once MethodSupportsCancellation
            try
            {
                service.ApplicationStopping().Wait();
            }
            catch (Exception ex)
            {
                try
                {
                    logger.LogError(ex, "Error occurred while stopping application.");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Error:" + ex);
                    Console.WriteLine("Error:" + ex2);
                }
            }
        });

        return Task.CompletedTask;
    }
}