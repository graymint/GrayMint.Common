namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    public static IServiceCollection AddGrayMintJob<T>(
        this IServiceCollection services,
        GrayMintJobOptions jobOptions) where T : IGrayMintJob
    {
        // AddHostedService would drop a second one without a word, its options with it
        if (services.Any(descriptor => descriptor.ServiceType == typeof(GrayMinJobService<T>)))
            throw new InvalidOperationException($"A job type can be added only once. JobName: {typeof(T).Name}");

        // Add GrayMinJobService
        services.AddSingleton(serviceProvider =>
        {
            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<T>>();
            return new GrayMinJobService<T>(serviceScopeFactory, logger, jobOptions);
        });
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<GrayMinJobService<T>>());

        return services;
    }
}
