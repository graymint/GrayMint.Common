using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintCommonExtension
{
    public static IServiceCollection AddGrayMintCommonServices(this IServiceCollection services,
        RegisterServicesOptions servicesOptions)
    {
        // CORS
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

        return services;
    }

    public static IApplicationBuilder UseGrayMintCommonServices(this WebApplication app, UseServicesOptions options)
    {
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
                // check if the request is *not* using the HTTPS scheme
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect(path);
                    return Task.CompletedTask;
                }

                // otherwise continue with the request pipeline
                return next();
            });

            return app;
        }
    }
}