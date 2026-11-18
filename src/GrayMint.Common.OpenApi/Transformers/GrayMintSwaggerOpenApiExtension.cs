using Scalar.AspNetCore;

namespace GrayMint.Common.OpenApi.Transformers;

internal static class GrayMintSwaggerOpenApiExtension
{
    public static IServiceCollection AddGrayMintOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddSchemaTransformer<PrimitiveTypeSchemaTransformer>();
        });

        return services;
    }

    public static IApplicationBuilder UseGrayMintOpenApi(this WebApplication app, string? title)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            if (!string.IsNullOrEmpty(title))
                options.WithTitle(title);
            options.WithClassicLayout();

            // a reader's browser calls only this service: no fonts from Scalar's CDN, no telemetry to Scalar
            options.DisableDefaultFonts();
            options.DisableTelemetry();
        });
        return app;
    }
}