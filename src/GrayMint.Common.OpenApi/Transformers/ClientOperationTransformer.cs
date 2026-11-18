using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// What a client generator such as NSwag's needs from a controller's operation to name and type it as NSwag's own
// document did:
// - the operation named Controller_Action, a ControllerClient class with an ActionAsync method;
// - a parameter without a default value required, and a string parameter a string, whatever its route constraint;
// - the body by its parameter's C# name, and a form as multipart/form-data, a method parameter per field;
// - an action that answers an IActionResult or a FileResult answering a file.
public class ClientOperationTransformer : IOpenApiOperationTransformer
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string MultipartContentType = "multipart/form-data";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        operation.OperationId = $"{action.ControllerName}_{action.ActionName}";

        foreach (var description in context.Description.ParameterDescriptions)
        {
            var parameterInfo = (description.ParameterDescriptor as ControllerParameterDescriptor)?.ParameterInfo;
            if (description.Source == BindingSource.Body)
                TransformBody(operation, description, parameterInfo);
            else if (FindParameter(operation, description) is { } parameter)
                TransformParameter(parameter, description, parameterInfo);
        }

        if (operation.RequestBody is OpenApiRequestBody { Content: { } content } &&
            content.TryGetValue(FormContentType, out var form) && !content.ContainsKey(MultipartContentType))
        {
            content.Remove(FormContentType);
            content[MultipartContentType] = form;
        }

        if (IsFileResult(action.MethodInfo.ReturnType))
            AddFileResponse(operation);

        return Task.CompletedTask;
    }

    private static void TransformBody(OpenApiOperation operation, ApiParameterDescription description,
        ParameterInfo? parameterInfo)
    {
        if (operation.RequestBody is not OpenApiRequestBody requestBody)
            return;

        requestBody.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        requestBody.Extensions["x-name"] = new JsonNodeExtension(JsonValue.Create(description.Name));
        if (parameterInfo is { HasDefaultValue: false })
            requestBody.Required = true;
    }

    private static void TransformParameter(OpenApiParameter parameter, ApiParameterDescription description,
        ParameterInfo? parameterInfo)
    {
        // a model's property is required by its own metadata; only a parameter of the action is required for having
        // no default value
        if (parameterInfo != null && description.ModelMetadata?.MetadataKind == ModelMetadataKind.Parameter)
        {
            if (!parameterInfo.HasDefaultValue)
                parameter.Required = true;

            if (parameterInfo.ParameterType == typeof(string) && parameter.Schema is OpenApiSchema schema)
            {
                schema.Type = JsonSchemaType.String;
                schema.Format = null;
            }

            // one that may be null says so, or a client checks that it is not
            if (IsNullable(parameterInfo))
            {
                if (parameter.Schema is OpenApiSchema { Type: { } schemaType } nullableSchema)
                    nullableSchema.Type = schemaType | JsonSchemaType.Null;
                else if (parameter.Schema is OpenApiSchemaReference reference)
                    parameter.Schema = new OpenApiSchema
                    {
                        OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, reference]
                    };
            }
        }
    }

    private static bool IsNullable(ParameterInfo parameterInfo)
    {
        var type = parameterInfo.ParameterType;
        if (type.IsValueType)
            return Nullable.GetUnderlyingType(type) != null;

        return new NullabilityInfoContext().Create(parameterInfo).ReadState != NullabilityState.NotNull;
    }

    private static OpenApiParameter? FindParameter(OpenApiOperation operation, ApiParameterDescription description)
    {
        return operation.Parameters?
            .OfType<OpenApiParameter>()
            .FirstOrDefault(x => string.Equals(x.Name, description.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddFileResponse(OpenApiOperation operation)
    {
        if (operation.Responses?.TryGetValue("200", out var okResponse) != true ||
            okResponse is not OpenApiResponse { Content: null or { Count: 0 } } response)
            return;

#if NET11_0_OR_GREATER // Microsoft.OpenApi 3 holds a media type by its interface
        response.Content ??= new Dictionary<string, IOpenApiMediaType>();
#else
        response.Content ??= new Dictionary<string, OpenApiMediaType>();
#endif
        response.Content["application/octet-stream"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
        };
    }

    private static bool IsFileResult(Type returnType)
    {
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() is var definition &&
            (definition == typeof(Task<>) || definition == typeof(ValueTask<>)))
            returnType = returnType.GenericTypeArguments[0];

        return returnType == typeof(IActionResult) || returnType == typeof(ActionResult) ||
               typeof(FileResult).IsAssignableFrom(returnType);
    }
}
