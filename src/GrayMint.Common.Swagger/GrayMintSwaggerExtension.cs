using GrayMint.Common.Swagger.NSwag;
using GrayMint.Common.Swagger.OpenApi;

namespace GrayMint.Common.Swagger;

public static class GrayMintSwaggerExtension
{
    private static AddSwaggerOptions? _options;

    public static IServiceCollection AddGrayMintSwagger(
        this IServiceCollection services,
        AddSwaggerOptions? options = null)
    {
        options ??= new AddSwaggerOptions();
        _options = options;

        // Add NSwag
        if (options.AddNSwag)
            services.AddGrayMintNSwag(options.Title);

        // Add OpenApi/Scalar last to override redirect uri
        if (options.AddScalar || options.AddOpenApi)
            services.AddGrayMintOpenApi();

        return services;
    }

    public static void UseGrayMintSwagger(
        this WebApplication app,
        UseSwaggerOptions? options = null)
    {
        options ??= new UseSwaggerOptions();

        // validation
        if (_options == null)
            throw new InvalidOperationException(
                "GrayMintSwaggerExtension is not initialized. Please call AddGrayMintSwagger in IServiceCollection first.");

        string? launchUrl = null;

        // Use NSwag
        if (_options.AddNSwag)
        {
            app.UseGrayMintNSwag();
            launchUrl = "/swagger/index.html";
        }

        // Use OpenApi/Scalar last to override redirect uri
        if (_options.AddScalar)
        {
            app.UseGrayMintOpenApi(_options.Title);
            launchUrl = "/scalar";
        }

        if (options.RedirectRootToSwaggerUi && !string.IsNullOrEmpty(launchUrl))
            app.RedirectRootToUrl(launchUrl);
    }

    private static void RedirectRootToUrl(this IApplicationBuilder app, string url)
    {
        app.Use((context, next) =>
        {
            // check if the request is *not* using the HTTPS scheme
            if (context.Request.Path == "/")
            {
                context.Response.Redirect(url);
                return Task.CompletedTask;
            }

            // otherwise continue with the request pipeline
            return next();
        });
    }
}