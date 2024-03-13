using GrayMint.Common.JobController;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    public static IServiceCollection AddGrayMintJob<T>(this IServiceCollection services,
        JobOptions jobOptions,
        int? maxDegreeOfParallelism = default)
        where T : IGrayMintJob
    {
        // Add GrayMinJobService
        services.AddHostedService(serviceProvider =>
        {
            jobOptions.Name ??= typeof(T).Name;
            var jobRunner = serviceProvider.GetRequiredService<JobRunner>();
            return new GrayMinJobService<T>(serviceProvider, jobOptions, jobRunner);
        });

        // Add JobRunner as singleton if not already added
        if (services.All(x => x.ServiceType != typeof(JobRunner)))
        {
            services.AddSingleton(serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<JobRunnerOptions>>();
                var logger = serviceProvider.GetRequiredService<ILogger<JobRunner>>();
                var jobRunner = new JobRunner(start: true, logger: logger)
                {
                    MaxDegreeOfParallelism = config.Value.MaxDegreeOfParallelism
                };
                return jobRunner;
            });
        }

        if (maxDegreeOfParallelism != null)
            services.Configure<JobRunnerOptions>((x) => x.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);

        return services;
    }
}