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
            RemoveUnusedSchemas(document, document.Components.Schemas);
        }

        NameSharedOperations(document);
        ClearStatusDescriptions(document);
    }

    private static void RemoveNullFromEnums(IDictionary<string, IOpenApiSchema> schemas)
    {
        foreach (var schema in schemas.Values.OfType<OpenApiSchema>())
        {
            // the values are typed never null, but the framework puts a null among them
            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (schema.Enum is not { Count: > 0 } values || !values.Any(x => x is null))
                continue;

            schema.Enum = values.Where(x => x != null).ToList();
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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

    private static void RemoveUnusedSchemas(OpenApiDocument document, IDictionary<string, IOpenApiSchema> schemas)
    {
        var used = new HashSet<string>();
        foreach (var operation in Operations(document).Select(x => x.Operation))
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
            while (true)
            {
                if (schema is OpenApiSchemaReference reference)
                {
                    if (reference.Reference.Id is not { } id || !used.Add(id))
                        return;

                    schema = schemas.TryGetValue(id, out var target) ? target : null;
                    continue;
                }

                if (schema is not OpenApiSchema openApiSchema)
                    return;

                foreach (var part in (openApiSchema.AllOf ?? []).Concat(openApiSchema.OneOf ?? [])
                             .Concat(openApiSchema.AnyOf ?? []).Concat(openApiSchema.Properties?.Values ?? []))
                    Visit(part);

                Visit(openApiSchema.Items);
                schema = openApiSchema.AdditionalProperties;
            }
        }
    }

    private static IEnumerable<DocumentOperation> Operations(OpenApiDocument document)
    {
        return document.Paths.SelectMany(path => path.Value.Operations?.Select(x =>
            new DocumentOperation { Path = path.Key, Method = x.Key, Operation = x.Value }) ?? []);
    }

    private static IEnumerable<IOpenApiSchema?> Schemas(IDictionary<string, IOpenApiMediaType>? content)
    {
        return content?.Values.Select(x => x.Schema) ?? [];
    }
}
