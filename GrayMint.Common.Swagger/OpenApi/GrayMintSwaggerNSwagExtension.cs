using Scalar.AspNetCore;

namespace GrayMint.Common.Swagger.OpenApi;

internal static class GrayMintSwaggerOpenApiExtension
{
    public static IServiceCollection AddGrayMintOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
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
        });
        return app;
    }
}