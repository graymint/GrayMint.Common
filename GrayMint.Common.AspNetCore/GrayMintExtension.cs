using System.Net;
using GrayMint.Common.AspNetCore.Services;
using GrayMint.Common.JobController;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintExtension
{
    public static void AddGrayMintCommonServices(this IServiceCollection services,
        GrayMintCommonOptions commonOptions,
        RegisterServicesOptions servicesOptions)
    {
        // configure settings
        commonOptions.Validate();
        services.AddSingleton(Options.Create(commonOptions));

        // cors
        if (servicesOptions.AddCors)
            services.AddCors(o => o.AddPolicy(GrayMintApp.CorsPolicyName, corsPolicyBuilder =>
            {
                corsPolicyBuilder
                    .SetIsOriginAllowed(_ => true) // allow any origin
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetPreflightMaxAge(TimeSpan.FromHours(24 * 30));
            }));

        // Add services to the container.
        if (servicesOptions.AddControllers)
            services.AddControllers(mvcOptions =>
            {
                mvcOptions.ModelMetadataDetailsProviders.Add(
                    new SuppressChildValidationMetadataProvider(typeof(IPAddress)));
            });

        if (servicesOptions.AddMemoryCache)
            services.AddMemoryCache();

        if (servicesOptions.AddHttpClient)
            services.AddHttpClient();

        services.AddHostedService<MaintenanceService>();
    }

    public static void UseGrayMintCommonServices(this WebApplication webApplication, UseServicesOptions options)
    {
        if (options.UseCors)
            webApplication.UseCors(GrayMintApp.CorsPolicyName);

        if (options.UseAuthentication)
            webApplication.UseAuthentication();

        if (options.UseAuthorization)
            webApplication.UseAuthorization();

        if (options.UseAppExceptions)
            webApplication.UseGrayMintExceptionHandler();

        if (options.MapControllers)
            webApplication.MapControllers();
    }

    public static void ScheduleGrayMintSqlMaintenance<TContext>(this WebApplication webApplication, TimeSpan interval) where TContext : DbContext
    {
        var maintenanceService = (MaintenanceService)webApplication.Services
            .GetRequiredService<IEnumerable<IHostedService>>()
            .Single(x=>x is MaintenanceService);

        maintenanceService.SqlMaintenanceJobs.Add(Tuple.Create(typeof(TContext), new JobSection(interval)));
    }


}