namespace GrayMint.Common.AspNetCore.ApplicationLifetime;

public static class GrayMintApplicationLifetimeExtensions
{
    public static IServiceCollection AddGrayMintApplicationLifetime<T>(this IServiceCollection services)
        where T : class, IGrayMintApplicationLifetime
    {
        services.AddHostedService<GrayMinApplicationLifetimeService<T>>();
        return services;
    }
}