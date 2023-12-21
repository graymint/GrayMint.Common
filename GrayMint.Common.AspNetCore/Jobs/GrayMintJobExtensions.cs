using GrayMint.Common.JobController;

namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    public static IServiceCollection AddGrayMintJob<T>(this IServiceCollection services, JobConfig jobConfig)
        where T : IGrayMintJob
    {
        services.AddHostedService((serviceProvider) => new GrayMinJobService<T>(serviceProvider, jobConfig));
        return services;
    }
}