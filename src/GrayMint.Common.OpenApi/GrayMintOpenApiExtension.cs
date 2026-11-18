using GrayMint.Common.OpenApi.Transformers;
using Scalar.AspNetCore;

namespace GrayMint.Common.OpenApi;

public static class GrayMintOpenApiExtension
{
    private static AddOpenApiOptions? _options;

    // ASP.NET Core's own OpenAPI document, at /openapi/v1.json, saying what NSwag's did, so a client generated from it
    // keeps its names and types
    public static IServiceCollection AddGrayMintOpenApi(
        this IServiceCollection services,
        AddOpenApiOptions? options = null)
    {
        options ??= new AddOpenApiOptions();
        _options = options;

        services.AddOpenApi(openApiOptions =>
        {
            openApiOptions.CreateSchemaReferenceId = ClientSchemaNames.CreateSchemaReferenceId;
            openApiOptions.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            openApiOptions.AddOperationTransformer<ClientOperationTransformer>();
            openApiOptions.AddSchemaTransformer<PrimitiveTypeSchemaTransformer>();
            openApiOptions.AddSchemaTransformer<ClientSchemaTransformer>();
        });

        return services;
    }

    public static void UseGrayMintOpenApi(
        this WebApplication app,
        UseOpenApiOptions? options = null)
    {
        options ??= new UseOpenApiOptions();

        // validation
        if (_options == null)
            throw new InvalidOperationException(
                "GrayMintOpenApiExtension is not initialized. Please call AddGrayMintOpenApi in IServiceCollection first.");

        app.MapOpenApi();
        if (!_options.AddScalar)
            return;

        var title = _options.Title;
        app.MapScalarApiReference(scalarOptions =>
        {
            if (!string.IsNullOrEmpty(title))
                scalarOptions.WithTitle(title);
            scalarOptions.WithClassicLayout();

            // a reader's browser calls only this service: no fonts from Scalar's CDN, no telemetry to Scalar
            scalarOptions.DisableDefaultFonts();
            scalarOptions.DisableTelemetry();
        });

        if (options.RedirectRootToUi)
            app.RedirectRootToUrl("/scalar");
    }

    private static void RedirectRootToUrl(this IApplicationBuilder app, string url)
    {
        app.Use((context, next) =>
        {
            if (context.Request.Path == "/")
            {
                context.Response.Redirect(url);
                return Task.CompletedTask;
            }

            return next();
        });
    }
}
