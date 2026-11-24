using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// A type's and a property's doc comments; an enum's lists its values with theirs.
internal sealed class XmlDocsSchemaTransformer : IOpenApiSchemaTransformer
{
    // The framework's own keys: a schema with an id becomes a component the property only refers to ($ref), and the
    // property's words then go beside that reference, as the component's own description is the type's.
    private const string SchemaIdKey = "x-schema-id";
    private const string RefDescriptionKey = "x-ref-description";

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        var isComponent = schema.Metadata?.TryGetValue(SchemaIdKey, out var schemaId) == true &&
                          schemaId is string { Length: > 0 };

        if (context.JsonPropertyInfo == null || isComponent)
        {
            schema.Description ??= type.IsEnum ? DescribeEnum(type) : XmlDocs.Summary(type);

            // an enum with a converter of its own hides its values from the framework; it writes their names, as the
            // converter does
            if (type.IsEnum && schema.Enum is not { Count: > 0 })
            {
                schema.Type ??= JsonSchemaType.String;
                schema.Enum = Enum.GetNames(type).Select(x => (JsonNode)x).ToArray();
            }
        }

        if (context.JsonPropertyInfo?.AttributeProvider is not MemberInfo member ||
            XmlDocs.Summary(member) is not { } text)
            return Task.CompletedTask;

        if (isComponent)
        {
            schema.Metadata ??= new Dictionary<string, object>();
            schema.Metadata.TryAdd(RefDescriptionKey, text);
        }
        else
        {
            schema.Description ??= text;
        }

        return Task.CompletedTask;
    }

    private static string? DescribeEnum(Type type)
    {
        var values = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => XmlDocs.Summary(field) is { } summary ? $"- `{field.Name}`: {summary}" : null)
            .OfType<string>()
            .ToArray();

        return XmlDocs.Join(XmlDocs.Summary(type), values.Length > 0 ? string.Join("\n", values) : null);
    }
}
