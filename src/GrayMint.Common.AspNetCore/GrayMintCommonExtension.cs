using System.Net;
using GrayMint.Common.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintCommonExtension
{
    public static IServiceCollection AddGrayMintCommonServices(this IServiceCollection services,
        RegisterServicesOptions servicesOptions)
    {
        if (servicesOptions.AddCors)
            services.AddCors(o => o.AddPolicy(GrayMintApp.CorsPolicyName, corsPolicyBuilder =>
            {
                corsPolicyBuilder
                    .SetIsOriginAllowed(_ => true) // allow any origin
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetPreflightMaxAge(TimeSpan.FromHours(24 * 30));
            }));

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

        return services;
    }

    public static IApplicationBuilder UseGrayMintCommonServices(this WebApplication app, UseServicesOptions options)
    {
        // what GmUtils' TryInvoke methods catch goes to the app's log while the app runs
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GrayMint.Common");
        GmUtils.Logger = logger;
        app.Lifetime.ApplicationStopped.Register(() => {
            if (GmUtils.Logger == logger)
                GmUtils.Logger = NullLogger.Instance;
        });

        if (options.UseCors)
            app.UseCors(GrayMintApp.CorsPolicyName);

        if (options.UseAppExceptions)
            app.UseGrayMintExceptionHandler();

        if (options.MapControllers)
            app.MapControllers();

        if (options.UseHttpsRedirection)
            app.UseHttpsRedirection();

        return app;
    }

    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder RedirectRoot(string path)
        {
            app.Use((context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect(path);
                    return Task.CompletedTask;
                }

                return next();
            });

            return app;
        }
    }
}