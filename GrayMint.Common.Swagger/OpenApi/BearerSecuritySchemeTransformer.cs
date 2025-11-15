using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.Swagger.OpenApi;

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document,
        OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Define the Bearer security scheme
        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste your JWT token here. Do not include the 'Bearer' prefix."
        };

        // Add it to document components
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = securityScheme
        };

        // Apply it as a requirement for all operations
        var operations = document.Paths.Values.Select(item => item.Operations ?? [])
            .SelectMany(operations => operations.Values);
        foreach (var operation in operations)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }

        return Task.CompletedTask;
    }
}