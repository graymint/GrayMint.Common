using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// Every operation takes a bearer token, but one its endpoint lets in without ([AllowAnonymous]). Registered twice: as
// an operation transformer it marks such an operation, which only an operation's context can tell, and as a document
// transformer, which runs after every operation's, it puts the requirement on the others.
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string AnonymousKey = "x-graymint-anonymous";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Metadata ??= new Dictionary<string, object>();
            operation.Metadata[AnonymousKey] = true;
        }

        return Task.CompletedTask;
    }

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

        // Apply it as a requirement for every operation that asks for a token
        var operations = document.Paths.Values.Select(item => item.Operations ?? [])
            .SelectMany(operations => operations.Values)
            .Where(operation => operation.Metadata?.ContainsKey(AnonymousKey) != true);
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
