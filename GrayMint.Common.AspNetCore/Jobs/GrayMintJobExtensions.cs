using GrayMint.Common.JobController;

namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    private static JobRunner? _jobRunner;

    public static IServiceCollection AddGrayMintJob<T>(this IServiceCollection services,
        JobConfig jobConfig,
        int maxDegreeOfParallelism = 1)
        where T : IGrayMintJob
    {
        services.AddHostedService((serviceProvider) =>
        {
            // create a single instance of JobRunner
            _jobRunner ??= new JobRunner(logger: serviceProvider.GetRequiredService<ILogger<JobRunner>>());
            _jobRunner.MaxDegreeOfParallelism = maxDegreeOfParallelism;

            return new GrayMinJobService<T>(serviceProvider, jobConfig, _jobRunner);
        });
        return services;
    }
}