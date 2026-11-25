using System.Text.Json.Serialization.Metadata;

namespace GrayMint.Common.OpenApi.Transformers;

// One name per type in a document, as NSwag names them: ClientSchemaNames' name, and a second type of the same name
// numbered after it (User, then User2). The framework's own name is the type's alone, so two types of one name would
// share a schema, and one of them would be described as the other. A file is no schema of its own: a client takes it
// as a file, and any JSON (a JsonElement) no schema either: a client takes it as any value, not as a class of that name.
internal sealed class ClientSchemaRegistry
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Type, string> _names = [];
    private readonly Dictionary<string, Type> _types = [];

    public string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        if (typeof(IFormFile).IsAssignableFrom(typeInfo.Type) || ClientSchemaBuilder.IsAnyValue(typeInfo.Type))
            return null;

        // a nullable value has its value's schema
        return ClientSchemaNames.CreateSchemaReferenceId(typeInfo) is { } name
            ? GetName(Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type, name)
            : null;
    }

    public Type? FindType(string name)
    {
        lock (_lock)
            return _types.GetValueOrDefault(name);
    }

    private string GetName(Type type, string name)
    {
        lock (_lock)
        {
            if (_names.TryGetValue(type, out var existing))
                return existing;

            var unique = name;
            for (var number = 2; _types.ContainsKey(unique); number++)
                unique = name + number;

            _names[type] = unique;
            _types[unique] = type;
            return unique;
        }
    }
}
