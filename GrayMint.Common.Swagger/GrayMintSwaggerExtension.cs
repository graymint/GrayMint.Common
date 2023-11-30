using System.Net;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using Asp.Versioning;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace GrayMint.Common.Swagger;

public static class GrayMintSwaggerExtension
{
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    public static IServiceCollection AddGrayMintSwagger(this IServiceCollection services, 
        string title, bool addVersioning)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerDocument(configure =>
        {
            configure.Title = title;
            configure.RequireParametersWithoutDefault = true;

            configure.SchemaSettings.TypeMappers = new List<ITypeMapper>
            {
                new PrimitiveTypeMapper(typeof(IPAddress), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(IPEndPoint), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(Version), s => { s.Type = JsonObjectType.String; }),
            };

            configure.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer"));
            configure.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = OpenApiSecuritySchemeType.ApiKey,
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the text-box: Bearer YOUR_JWT"
            });
        });

        // Version
        if (addVersioning)
        {
            services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                })
                .AddApiExplorer(options =>
                {
                    // ReSharper disable once StringLiteralTypo
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });
        }

        return services;

    }

    public static IApplicationBuilder UseGrayMintSwagger(this IApplicationBuilder app, bool redirectRootToSwaggerUi = false)
    {
        app.UseOpenApi();
        app.UseSwaggerUi();

        if (redirectRootToSwaggerUi)
        {
            app.Use(async (context, next) =>
            {
                // check if the request is *not* using the HTTPS scheme
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/swagger/index.html");
                    return;
                }

                // otherwise continue with the request pipeline
                await next();
            });
        }

        return app;
    }
}