namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    public static IServiceCollection AddGrayMintJob<T>(
        this IServiceCollection services,
        GrayMintJobOptions jobOptions) where T : IGrayMintJob
    {
        // AddHostedService would drop a second one without a word, its options with it
        if (services.Any(descriptor => descriptor.ServiceType == typeof(GrayMintJobService<T>)))
            throw new InvalidOperationException($"A job type can be added only once. JobName: {typeof(T).Name}");

        services.AddSingleton(serviceProvider =>
        {
            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<T>>();
            return new GrayMintJobService<T>(serviceScopeFactory, logger, jobOptions);
        });

        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<GrayMintJobService<T>>());

        return services;
    }
}
