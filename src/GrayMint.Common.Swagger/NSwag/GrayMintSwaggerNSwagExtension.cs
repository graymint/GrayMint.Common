using System.Net;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace GrayMint.Common.Swagger.NSwag;

internal static class GrayMintSwaggerNSwagExtension
{
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    public static IServiceCollection AddGrayMintNSwag(this IServiceCollection services, string? title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerDocument(configure =>
        {
            if (title != null) configure.Title = title;
            configure.RequireParametersWithoutDefault = true;
            configure.SchemaSettings.TypeMappers = new List<ITypeMapper>
            {
                new PrimitiveTypeMapper(typeof(IPAddress), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(IPEndPoint), s => { s.Type = JsonObjectType.String; }),
                new PrimitiveTypeMapper(typeof(Version), s => { s.Type = JsonObjectType.String; })
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

        return services;
    }

    public static IApplicationBuilder UseGrayMintNSwag(this IApplicationBuilder app)
    {
        app.UseOpenApi();
        app.UseSwaggerUi();
        return app;
    }
}