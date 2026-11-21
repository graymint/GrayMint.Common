using GrayMint.Common.OpenApi.Transformers;
using Scalar.AspNetCore;

namespace GrayMint.Common.OpenApi;

public static class GrayMintOpenApiExtension
{
    private const string ScalarPrefix = "/scalar";
    private static AddOpenApiOptions? _options;

    // ASP.NET Core's own OpenAPI document, at /openapi/v1.json, saying what NSwag's did, so a client generated from it
    // keeps its names and types, with the words of the app's doc comments. An app's own transformers, added to the
    // "v1" document after this call, run after these.
    public static IServiceCollection AddGrayMintOpenApi(
        this IServiceCollection services,
        AddOpenApiOptions? options = null)
    {
        options ??= new AddOpenApiOptions();
        _options = options;

        services.AddOpenApi(openApiOptions =>
        {
            var schemaRegistry = new ClientSchemaRegistry();
            openApiOptions.CreateSchemaReferenceId = schemaRegistry.CreateSchemaReferenceId;
            openApiOptions.AddOperationTransformer<BearerSecuritySchemeTransformer>();
            openApiOptions.AddOperationTransformer<ClientOperationTransformer>();
            openApiOptions.AddOperationTransformer<XmlDocsOperationTransformer>();
            openApiOptions.AddSchemaTransformer<PrimitiveTypeSchemaTransformer>();
            openApiOptions.AddSchemaTransformer<ClientSchemaTransformer>();
            openApiOptions.AddSchemaTransformer<XmlDocsSchemaTransformer>();
            openApiOptions.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            openApiOptions.AddDocumentTransformer(new ApiDocsDocumentTransformer(options));
            openApiOptions.AddDocumentTransformer(new ClientDocumentTransformer(schemaRegistry));
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
        app.MapScalarApiReference(ScalarPrefix, scalarOptions =>
        {
            if (!string.IsNullOrEmpty(title))
                scalarOptions.WithTitle(title);
            scalarOptions.WithClassicLayout();

            // a reader's browser calls only this service: Scalar's fonts come from it, not from Scalar's CDN, and no
            // telemetry goes to Scalar
            scalarOptions.DisableDefaultFonts();
            scalarOptions.WithCustomCss(ScalarFonts.Css);
            scalarOptions.DisableTelemetry();
        });
        app.MapScalarFonts(ScalarPrefix);

        if (options.RedirectRootToUi)
            app.RedirectRootToUrl(ScalarPrefix);
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
