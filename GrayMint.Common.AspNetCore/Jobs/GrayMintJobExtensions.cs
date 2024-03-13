using GrayMint.Common.JobController;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Jobs;

public static class GrayMintJobExtensions
{
    private static TimeSpan? _minInterval;
    public static IServiceCollection AddGrayMintJob<T>(this IServiceCollection services,
        GrayMintJobOptions jobOptions,
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

                // set jobRunner Interval
                if (_minInterval  < jobRunner.Interval)
                    jobRunner.Interval = _minInterval.Value;

                return jobRunner;
            });
        }

        // configure JobRunnerOptions
        if (maxDegreeOfParallelism != null)
            services.Configure<JobRunnerOptions>(x => x.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);

        if (_minInterval == null || _minInterval < jobOptions.Interval)
            _minInterval = jobOptions.Interval;

        return services;
    }
}