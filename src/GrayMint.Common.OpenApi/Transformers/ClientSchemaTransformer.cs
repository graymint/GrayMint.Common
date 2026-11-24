using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// What a client generator such as NSwag's needs from a schema to type it as NSwag's own document did:
// - A property that is never null is required. OpenAPI marks a property required only when C# declares it 'required',
//   so a merely non-nullable member (a computed property, a value type) would be optional in a client, while it is
//   always sent; nullability alone says "may be null".
// - An obsolete property is left out.
// - An object takes no properties but its own, or a client gets an extra dictionary in every class.
// - A number is a number: ASP.NET Core's web defaults also read one from a string, and the document says so with a
//   string pattern a client would check.
public class ClientSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Type is { } type && (type & (JsonSchemaType.Integer | JsonSchemaType.Number)) != 0 &&
            (type & JsonSchemaType.String) != 0)
        {
            schema.Type = type & ~JsonSchemaType.String;
            schema.Pattern = null;
        }

        // an enum of names is a string, which the document leaves unsaid; its values are typed never null, but the
        // framework puts a null among them
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (schema.Type == null && schema.Enum is { Count: > 0 } values &&
            values.All(x => x == null || x.GetValueKind() == JsonValueKind.String))
            schema.Type = values.Any(x => x == null)
                ? JsonSchemaType.String | JsonSchemaType.Null
                : JsonSchemaType.String;

        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        // a dictionary of any value says it has values, or a client takes it for a plain object
        if (context.JsonTypeInfo.Kind == JsonTypeInfoKind.Dictionary && schema.AdditionalProperties == null)
            schema.AdditionalProperties = new OpenApiSchema();

        // a property of any value that may be null: an empty schema allows null already, but NSwag's TypeScript says
        // so only for OpenAPI 3.0's nullable, which it reads in any document
        if (context.JsonTypeInfo.Type == typeof(object) && context.JsonPropertyInfo is { } anyProperty &&
            IsNullable(anyProperty.PropertyType, anyProperty.AttributeProvider as MemberInfo, new NullabilityInfoContext()))
        {
            schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            schema.Extensions["nullable"] = new JsonNodeExtension(JsonValue.Create(true));
        }

        // an object's schema, whether the generator met its type first by itself or as a property's type
        if (context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            return Task.CompletedTask;

        schema.AdditionalPropertiesAllowed = false;
        if (schema.Properties == null)
            return Task.CompletedTask;

        var nullabilityContext = new NullabilityInfoContext(); // not thread-safe, so one per call
        foreach (var property in context.JsonTypeInfo.Properties)
        {
            if (!schema.Properties.ContainsKey(property.Name))
                continue;

            var member = property.AttributeProvider as MemberInfo;
            if (member?.IsDefined(typeof(ObsoleteAttribute), inherit: true) == true)
            {
                schema.Properties.Remove(property.Name);
                schema.Required?.Remove(property.Name);
                continue;
            }

            // required as NSwag had it: never null, or declared required; not for being a constructor's parameter
            if (!IsNullable(property.PropertyType, member, nullabilityContext) || property.IsRequired ||
                member?.IsDefined(typeof(RequiredAttribute), inherit: true) == true)
                (schema.Required ??= new HashSet<string>()).Add(property.Name);
            else
                schema.Required?.Remove(property.Name);
        }

        return Task.CompletedTask;
    }

    internal static bool IsNullable(Type type, MemberInfo? member, NullabilityInfoContext nullabilityContext)
    {
        if (type.IsValueType)
            return Nullable.GetUnderlyingType(type) != null;

        return member switch
        {
            PropertyInfo propertyInfo => nullabilityContext.Create(propertyInfo).ReadState != NullabilityState.NotNull,
            FieldInfo fieldInfo => nullabilityContext.Create(fieldInfo).ReadState != NullabilityState.NotNull,
            _ => true
        };
    }
}
