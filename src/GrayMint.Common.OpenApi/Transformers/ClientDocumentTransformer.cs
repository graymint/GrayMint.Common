using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// What a client generator such as NSwag's needs from the whole document to name and type its code as NSwag's own
// document did; it runs after every other transformer:
// - the schemas the framework leaves empty, the nullable references and the base classes (ClientSchemaBuilder);
// - an enum's schema without null: the framework adds it when a nullable use comes first, and every use then reads
//   the enum as nullable;
// - one action's several methods each with the method after its operation id (Provider_ReturnGET, Provider_ReturnPOST);
// - a response with no words of its own without its status's name, which a client would take as its words;
// - no schema that no operation uses, of which a client would make a class.
// The schemas' order is not NSwag's: the writer sorts them by name, so a client's classes come in that order.
internal sealed class ClientDocumentTransformer(ClientSchemaRegistry registry) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas != null)
        {
            var builder = new ClientSchemaBuilder(document, context, registry);
            await builder.RepairEmptySchemas(cancellationToken);
            builder.MarkNullableReferences();
            await builder.AddBaseClasses(cancellationToken);
            RemoveNullFromEnums(document.Components.Schemas);
            RemoveUnusedSchemas(document);
        }

        NameSharedOperations(document);
        ClearStatusDescriptions(document);
    }

    private static void RemoveNullFromEnums(IDictionary<string, IOpenApiSchema> schemas)
    {
        foreach (var schema in schemas.Values.OfType<OpenApiSchema>())
        {
            if (schema.Enum is not { Count: > 0 } values || !values.Any(x => x is null))
                continue;

            schema.Enum = values.Where(x => x != null).ToList();
            if (schema.Type is { } type)
                schema.Type = type & ~JsonSchemaType.Null;
        }
    }

    private static void NameSharedOperations(OpenApiDocument document)
    {
        var operations = Operations(document)
            .Where(x => x.Operation.OperationId != null)
            .GroupBy(x => x.Operation.OperationId)
            .Where(x => x.Count() > 1 && x.Select(y => y.Path).Distinct().Count() == 1);

        foreach (var operation in operations.SelectMany(x => x))
            operation.Operation.OperationId += operation.Method.Method.ToUpperInvariant();
    }

    private static void ClearStatusDescriptions(OpenApiDocument document)
    {
        var responses = Operations(document).SelectMany(x => x.Operation.Responses ?? []);
        foreach (var (status, response) in responses)
        {
            if (response is OpenApiResponse openApiResponse && int.TryParse(status, out var code) &&
                openApiResponse.Description == ReasonPhrases.GetReasonPhrase(code))
                openApiResponse.Description = string.Empty;
        }
    }

    private static void RemoveUnusedSchemas(OpenApiDocument document)
    {
        var schemas = document.Components!.Schemas!;
        var used = new HashSet<string>();
        foreach (var (_, _, operation) in Operations(document))
        {
            foreach (var response in operation.Responses?.Values.AsEnumerable() ?? [])
                foreach (var schema in Schemas(response.Content))
                    Visit(schema);
            foreach (var parameter in operation.Parameters ?? [])
                Visit(parameter.Schema);
            foreach (var schema in Schemas(operation.RequestBody?.Content))
                Visit(schema);
        }

        foreach (var name in schemas.Keys.Where(x => !used.Contains(x)).ToArray())
            schemas.Remove(name);
        return;

        void Visit(IOpenApiSchema? schema)
        {
            if (schema is OpenApiSchemaReference reference)
            {
                if (reference.Reference.Id is { } id && used.Add(id))
                    Visit(schemas.TryGetValue(id, out var target) ? target : null);
                return;
            }

            if (schema is not OpenApiSchema openApiSchema)
                return;

            foreach (var part in (openApiSchema.AllOf ?? []).Concat(openApiSchema.OneOf ?? [])
                         .Concat(openApiSchema.AnyOf ?? []).Concat(openApiSchema.Properties?.Values ?? []))
                Visit(part);
            Visit(openApiSchema.Items);
            Visit(openApiSchema.AdditionalProperties);
        }
    }

    private static IEnumerable<(string Path, HttpMethod Method, OpenApiOperation Operation)> Operations(
        OpenApiDocument document)
    {
        return document.Paths.SelectMany(path =>
            path.Value.Operations?.Select(x => (path.Key, x.Key, x.Value)) ?? []);
    }

#if NET11_0_OR_GREATER // Microsoft.OpenApi 3 holds a media type by its interface
    private static IEnumerable<IOpenApiSchema?> Schemas(IDictionary<string, IOpenApiMediaType>? content)
#else
    private static IEnumerable<IOpenApiSchema?> Schemas(IDictionary<string, OpenApiMediaType>? content)
#endif
    {
        return content?.Values.Select(x => x.Schema) ?? [];
    }
}
