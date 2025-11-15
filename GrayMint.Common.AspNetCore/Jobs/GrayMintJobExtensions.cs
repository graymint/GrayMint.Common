using GrayMint.Common.Jobs;

namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    public static IServiceCollection AddGrayMintJob<T>(
        this IServiceCollection services,
        GrayMintJobOptions jobOptions,
        JobRunner? jobRunner = null) where T : IGrayMintJob
    {
        // Add GrayMinJobService
        services.AddHostedService(serviceProvider =>
        {
            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            return new GrayMinJobService<T>(serviceScopeFactory, jobOptions, jobRunner);
        });

        return services;
    }
}