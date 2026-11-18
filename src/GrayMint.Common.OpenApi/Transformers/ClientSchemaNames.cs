using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// A generic type's schema named as NSwag names it, so a client generated from the document keeps its type names:
// Patch<int?> is PatchOfNullableInteger, Patch<AccessPoint[]> PatchOfAccessPointOf, Result<A, B> ResultOfAAndB.
// Any other type keeps the default name.
public static class ClientSchemaNames
{
    public static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;
        return typeInfo.Kind == JsonTypeInfoKind.Object && type.IsConstructedGenericType &&
               Nullable.GetUnderlyingType(type) == null
            ? GetName(type)
            : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }

    private static string GetName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return "Nullable" + GetName(underlyingType);

        if (type.IsArray)
            return GetName(type.GetElementType()!) + "Of";

        // its own name, not its underlying integer's
        if (type.IsEnum)
            return type.Name;

        if (type.IsConstructedGenericType)
            return type.Name[..type.Name.IndexOf('`')] + "Of" +
                   string.Join("And", type.GenericTypeArguments.Select(GetName));

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "Boolean",
            TypeCode.Byte => "Byte",
            TypeCode.Int16 => "Short",
            TypeCode.Int32 => "Integer",
            TypeCode.Int64 => "Long",
            TypeCode.Single => "Float",
            TypeCode.Double => "Double",
            TypeCode.Decimal => "Decimal",
            TypeCode.String => "String",
            _ => type.Name
        };
    }
}
