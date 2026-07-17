using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.Swagger.OpenApi;

public class PrimitiveTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type == typeof(IPAddress))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "ip"; // supports both IPv4 and IPv6
            schema.Example = JsonValue.Create("192.168.1.10 or 2001:db8::1");
        }
        else if (type == typeof(IPEndPoint))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "ip:port"; // mixed form
            schema.Example = JsonValue.Create("192.168.1.10:8080 or [2001:db8::1]:443");
        }
        else if (type == typeof(Version))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "semver";
            schema.Example = JsonValue.Create("1.2.3.4");
        }
        else
        {
            return Task.CompletedTask;
        }

        schema.Properties?.Clear();
        schema.AllOf?.Clear();
        schema.OneOf?.Clear();
        schema.AnyOf?.Clear();
        return Task.CompletedTask;
    }
}