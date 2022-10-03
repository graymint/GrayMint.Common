using System.Net;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace GrayMint.Common.AspNetCore;

public static class AppSwaggerExtension
{
    public static IServiceCollection AddAppSwagger(this IServiceCollection services, string title)
    {
        services.AddSwaggerDocument(configure =>
        {
            configure.Title = title;

            configure.TypeMappers = new List<ITypeMapper>
            {
                new PrimitiveTypeMapper(typeof(IPAddress), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(IPEndPoint), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(Version), s => { s.Type = JsonObjectType.String; }),
            };

            configure.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer"));
            configure.AddSecurity("Bearer", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = OpenApiSecuritySchemeType.ApiKey,
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the text-box: Bearer YOUR_JWT"
            });
        });

        // Version
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        services.AddVersionedApiExplorer(options =>
        {
            // ReSharper disable once StringLiteralTypo
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });


        return services;

    }
}