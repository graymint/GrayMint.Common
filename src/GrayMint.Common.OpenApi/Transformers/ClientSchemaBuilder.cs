using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace GrayMint.Common.OpenApi.Transformers;

// A document's schemas made as NSwag made them, where the framework's differ:
// - one the framework leaves empty is built again: a type it first meets in a collection, with a property back to a
//   type above it, comes out as {} with no schema of its own;
// - a class with a base class has the base's schema and its own fields (allOf), so a client's class keeps the base;
// - a property that may be null and refers to another schema is nullable, also without nullable annotations.
// A class is a schema of its own the others refer to, a collection an array of its element's, a dictionary an object
// of its value's, and anything else the framework's own schema.
internal sealed class ClientSchemaBuilder(
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    ClientSchemaRegistry registry)
{
    private readonly JsonSerializerOptions _jsonOptions = context.ApplicationServices
        .GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

    private readonly NullabilityInfoContext _nullabilityContext = new();

    private IDictionary<string, IOpenApiSchema> Schemas =>
        document.Components?.Schemas ?? throw new InvalidOperationException("The document has no schemas.");

    public async Task RepairEmptySchemas(CancellationToken cancellationToken)
    {
        foreach (var (name, component) in Schemas.ToArray())
        {
            if (component is not OpenApiSchema schema || registry.FindType(name) is not { } type ||
                _jsonOptions.GetTypeInfo(type) is not { Kind: JsonTypeInfoKind.Object } typeInfo)
                continue;

            if (IsEmptyObject(schema) && typeInfo.Properties.Count > 0)
            {
                await FillObject(schema, type, cancellationToken);
                continue;
            }

            foreach (var property in typeInfo.Properties)
            {
                if (schema.Properties?.TryGetValue(property.Name, out var propertySchema) != true ||
                    !IsEmpty(propertySchema) || IsAnyValue(property.PropertyType))
                    continue;

                var rebuilt = await BuildPropertySchema(property, cancellationToken);
                if (rebuilt is OpenApiSchema rebuiltSchema)
                    rebuiltSchema.Description ??= propertySchema?.Description;
                schema.Properties[property.Name] = rebuilt;
            }
        }
    }

    public async Task AddBaseClasses(CancellationToken cancellationToken)
    {
        // a base the document did not have yet joins the list, and may have a base of its own
        var done = new HashSet<string>();
        while (Schemas.Keys.FirstOrDefault(x => !done.Contains(x)) is { } name)
        {
            done.Add(name);
            if (Schemas[name] is not OpenApiSchema { Properties.Count: > 0 } schema ||
                registry.FindType(name) is not { } type || !HasBaseSchema(type))
                continue;

            var baseType = type.BaseType!;
            var baseReference = await GetComponent(baseType, cancellationToken);
            var baseProperties = _jsonOptions.GetTypeInfo(baseType).Properties.Select(x => x.Name).ToHashSet();
            var own = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = schema.Description,
                AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
                Required = schema.Required?.Where(x => !baseProperties.Contains(x)).ToHashSet(),
                Properties = schema.Properties
                    .Where(x => !baseProperties.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value)
            };

            schema.Type = null;
            schema.Description = null;
            schema.AdditionalPropertiesAllowed = true;
            schema.Required = null;
            schema.Properties = null;
            schema.AllOf = [baseReference, own];
        }
    }

    // a class NSwag gave its base's schema: one whose base is a class of the app or a package with fields of its own,
    // not one the framework describes by its derived types
    private static bool HasBaseSchema(Type type)
    {
        return type.BaseType is { IsClass: true } baseType && baseType != typeof(object) &&
               baseType.Namespace?.StartsWith("System", StringComparison.Ordinal) == false &&
               baseType.Namespace?.StartsWith("Microsoft", StringComparison.Ordinal) == false &&
               !type.IsDefined(typeof(JsonPolymorphicAttribute), inherit: true) &&
               !baseType.IsDefined(typeof(JsonPolymorphicAttribute), inherit: true) &&
               baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length > 0;
    }

    private Task<IOpenApiSchema> BuildPropertySchema(JsonPropertyInfo property, CancellationToken cancellationToken)
    {
        var member = property.AttributeProvider as MemberInfo;
        return BuildSchema(property.PropertyType, IsNullable(property.PropertyType, member), Nullability(member),
            cancellationToken);
    }

    private async Task<IOpenApiSchema> BuildSchema(Type type, bool nullable, NullabilityInfo? nullability,
        CancellationToken cancellationToken)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
        {
            type = underlyingType;
            nullable = true;
        }

        var typeInfo = _jsonOptions.GetTypeInfo(type);
        if (typeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary &&
            typeInfo.ElementType is { } elementType)
        {
            var elementNullability = nullability?.ElementType ?? nullability?.GenericTypeArguments.LastOrDefault();
            var element = await BuildSchema(elementType, IsNullable(elementType, elementNullability),
                elementNullability, cancellationToken);
            var kind = typeInfo.Kind == JsonTypeInfoKind.Enumerable ? JsonSchemaType.Array : JsonSchemaType.Object;
            return typeInfo.Kind == JsonTypeInfoKind.Enumerable
                ? new OpenApiSchema { Type = kind | NullFlag(nullable), Items = element }
                : new OpenApiSchema { Type = kind | NullFlag(nullable), AdditionalProperties = element };
        }

        if (registry.CreateSchemaReferenceId(typeInfo) != null)
        {
            var reference = await GetComponent(type, cancellationToken);
            return nullable
                ? new OpenApiSchema { OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, reference] }
                : reference;
        }

        var schema = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
        if (nullable && schema is OpenApiSchema { Type: { } schemaType } nullableSchema)
            nullableSchema.Type = schemaType | JsonSchemaType.Null;
        return schema;
    }

    // a class's schema of its own: the document's, or one built here when the document has none or an empty one
    private async Task<IOpenApiSchema> GetComponent(Type type, CancellationToken cancellationToken)
    {
        var typeInfo = _jsonOptions.GetTypeInfo(type);
        var name = registry.CreateSchemaReferenceId(typeInfo) ??
                   throw new InvalidOperationException($"{type.Name} has no schema of its own.");

        if (!Schemas.TryGetValue(name, out var existing))
        {
            // the framework's own schema for a value, such as an enum or a class written as a string (IPAddress); a
            // class's fields are built here, as the framework's schema holds its references' whole schemas
            var framework = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
            if (framework is OpenApiSchema frameworkSchema && IsObject(frameworkSchema))
            {
                var schema = new OpenApiSchema();
                document.AddComponent(name, schema);
                await FillObject(schema, type, cancellationToken);
            }
            else
            {
                document.AddComponent(name, framework);
            }
        }
        else if (existing is OpenApiSchema empty && IsEmptyObject(empty) &&
                 typeInfo is { Kind: JsonTypeInfoKind.Object, Properties.Count: > 0 })
        {
            await FillObject(empty, type, cancellationToken);
        }

        return new OpenApiSchemaReference(name, document);
    }

    private async Task FillObject(OpenApiSchema schema, Type type, CancellationToken cancellationToken)
    {
        schema.Type = JsonSchemaType.Object;
        schema.AdditionalPropertiesAllowed = false;
        schema.Description ??= XmlDocs.Summary(type);
        schema.Properties = new Dictionary<string, IOpenApiSchema>();
        schema.Required = new HashSet<string>();

        // a property without a getter, as one [JsonIgnore] hides, is never written
        foreach (var property in _jsonOptions.GetTypeInfo(type).Properties.Where(x => x.Get != null))
        {
            var member = property.AttributeProvider as MemberInfo;
            if (member?.IsDefined(typeof(ObsoleteAttribute), inherit: true) == true)
                continue;

            var propertySchema = await BuildPropertySchema(property, cancellationToken);
            if (member != null && XmlDocs.Summary(member) is { } text)
                Describe(propertySchema, text);
            schema.Properties[property.Name] = propertySchema;

            if (!IsNullable(property.PropertyType, member) || property.IsRequired ||
                member?.IsDefined(typeof(RequiredAttribute), inherit: true) == true)
                schema.Required.Add(property.Name);
        }
    }

    // a property's words: beside its reference, whose own Description falls back to the type's, or on its schema
    private static void Describe(IOpenApiSchema schema, string text)
    {
        if (schema is OpenApiSchemaReference reference)
            reference.Description = reference.Reference.Description ?? text;
        else if (schema is OpenApiSchema inlineSchema)
            inlineSchema.Description ??= text;
    }

    // a property that may be null and refers to a schema of its own says so: the framework writes the bare reference
    // for one without nullable annotations, which NSwag read as nullable
    public void MarkNullableReferences()
    {
        foreach (var (name, component) in Schemas)
        {
            if (component is not OpenApiSchema { Properties: { } properties } || registry.FindType(name) is not { } type)
                continue;

            foreach (var property in _jsonOptions.GetTypeInfo(type).Properties)
            {
                if (properties.TryGetValue(property.Name, out var propertySchema) &&
                    propertySchema is OpenApiSchemaReference reference &&
                    IsNullable(property.PropertyType, property.AttributeProvider as MemberInfo))
                {
                    properties[property.Name] = new OpenApiSchema
                    {
                        Description = reference.Reference.Description,
                        OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, reference]
                    };
                    reference.Description = null;
                }
            }
        }
    }

    private NullabilityInfo? Nullability(MemberInfo? member)
    {
        return member switch
        {
            PropertyInfo propertyInfo => _nullabilityContext.Create(propertyInfo),
            FieldInfo fieldInfo => _nullabilityContext.Create(fieldInfo),
            _ => null
        };
    }

    private bool IsNullable(Type type, MemberInfo? member)
    {
        return ClientSchemaTransformer.IsNullable(type, member, _nullabilityContext);
    }

    private static bool IsNullable(Type type, NullabilityInfo? nullability)
    {
        return type.IsValueType
            ? Nullable.GetUnderlyingType(type) != null
            : nullability?.ReadState != NullabilityState.NotNull;
    }

    private static JsonSchemaType NullFlag(bool nullable)
    {
        return nullable ? JsonSchemaType.Null : 0;
    }

    private static bool IsObject(OpenApiSchema schema)
    {
        return schema.Type is { } type
            ? (type & ~JsonSchemaType.Null) == JsonSchemaType.Object
            : schema.Properties is { Count: > 0 };
    }

    // an object's schema the framework left without its fields; a class written as a value (IPAddress as a string)
    // has none of its own either, but a type that says so
    private static bool IsEmptyObject(OpenApiSchema schema)
    {
        return (schema.Type == null || (schema.Type & ~JsonSchemaType.Null) == JsonSchemaType.Object) &&
               schema is
               {
                   Format: null, Enum: null or { Count: 0 }, Items: null, Properties: null or { Count: 0 },
                   OneOf: null or { Count: 0 }, AllOf: null or { Count: 0 }, AnyOf: null or { Count: 0 }
               };
    }

    private static bool IsEmpty(IOpenApiSchema? schema)
    {
        return schema is OpenApiSchema
        {
            Type: null, Format: null, Enum: null or { Count: 0 }, Items: null, AdditionalProperties: null,
            Properties: null or { Count: 0 }, OneOf: null or { Count: 0 }, AllOf: null or { Count: 0 },
            AnyOf: null or { Count: 0 }, Not: null, Const: null
        };
    }

    // a property of any JSON, whose schema is empty on purpose
    private static bool IsAnyValue(Type type)
    {
        return type == typeof(object) || type == typeof(JsonElement) || type == typeof(JsonElement?) ||
               typeof(JsonNode).IsAssignableFrom(type);
    }
}
