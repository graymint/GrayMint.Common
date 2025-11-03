using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Net;

namespace GrayMint.Common.Swagger.OpenApi;

public class PrimitiveTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync2(
        OpenApiSchema schema, 
        OpenApiSchemaTransformerContext context, 
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type == typeof(IPAddress) || type == typeof(IPEndPoint) || type == typeof(Version))
        {
            schema.Type = "string";
            schema.Format = "ip"; // supports both IPv4 and IPv6
            schema.Example = new OpenApiString("192.168.1.10 or 2001:db8::1");
            schema.Properties?.Clear();
            schema.AllOf?.Clear();
            schema.OneOf?.Clear();
            schema.AnyOf?.Clear();
        }

        return Task.CompletedTask;
    }

    public Task TransformAsync(
        OpenApiSchema schema, 
        OpenApiSchemaTransformerContext context, 
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type == typeof(IPAddress))
        {
            schema.Type = "string";
            schema.Format = "ip"; // supports both IPv4 and IPv6
            schema.Example = new OpenApiString("192.168.1.10 or 2001:db8::1");
        }
        else if (type == typeof(IPEndPoint))
        {
            schema.Type = "string";
            schema.Format = "ip:port"; // mixed form
            schema.Example = new OpenApiString("192.168.1.10:8080 or [2001:db8::1]:443");
        }
        else if (type == typeof(Version))
        {
            schema.Type = "string";
            schema.Format = "semver";
            schema.Example = new OpenApiString("1.2.3.4");
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
