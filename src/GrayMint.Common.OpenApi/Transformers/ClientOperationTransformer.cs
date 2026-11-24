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
// - the body by its parameter's C# name, and a form as multipart/form-data, a method parameter per field, a file
//   among them optional;
// - each parameter and the body at its place among the action's described parameters (x-position), which orders a
//   method's parameters;
// - an action that answers a result it writes itself (an IActionResult) answering a file;
// - a nullable value answered as the value's schema, and a nullable collection's items nullable, as NSwag read them.
public class ClientOperationTransformer : IOpenApiOperationTransformer
{
    private const string FormContentType = "application/x-www-form-urlencoded";
    private const string MultipartContentType = "multipart/form-data";
    private const string PositionKey = "x-position";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        operation.OperationId = $"{action.ControllerName}_{action.ActionName}";

        // NSwag's place of each parameter, from 1, among those the action is described with (x-position): a client's
        // method orders its parameters by it, the body among them
        var position = 0;
        foreach (var description in context.Description.ParameterDescriptions)
        {
            position++;
            var parameterInfo = (description.ParameterDescriptor as ControllerParameterDescriptor)?.ParameterInfo;
            if (description.Source == BindingSource.Body)
                TransformBody(operation, description, parameterInfo, position);
            else if (FindParameter(operation, description) is { } parameter)
                TransformParameter(parameter, description, parameterInfo, position);
        }

        if (operation.RequestBody is OpenApiRequestBody { Content: { } content } &&
            content.TryGetValue(FormContentType, out var form) && !content.ContainsKey(MultipartContentType))
        {
            content.Remove(FormContentType);
            content[MultipartContentType] = form;
        }

        if (operation.RequestBody is OpenApiRequestBody { Content: { } formContent } &&
            formContent.TryGetValue(MultipartContentType, out var multipart) && multipart.Schema is OpenApiSchema fields)
            TransformFormFiles(fields);

        if (IsFileResult(action.MethodInfo.ReturnType))
            AddFileResponse(operation);

        TransformResponses(operation, context.Description, action.MethodInfo);
        return Task.CompletedTask;
    }

    // a form's file as NSwag had it: a file a client may leave out, whatever the model says, so its method takes it
    // as an optional file
    private static void TransformFormFiles(OpenApiSchema fields)
    {
        if (fields.Properties is not { } properties)
            return;

        foreach (var (name, field) in properties.ToArray())
        {
            var isFile = field is OpenApiSchemaReference { Target.Format: "binary" } or
                OpenApiSchema { Format: "binary" };

            if (!isFile)
                continue;

            properties[name] = new OpenApiSchema
            {
                Type = JsonSchemaType.String | JsonSchemaType.Null,
                Format = "binary"
            };

            fields.Required?.Remove(name);
        }
    }

    private static void TransformResponses(OpenApiOperation operation, ApiDescription description, MethodInfo method)
    {
        if (operation.Responses is null)
            return;

        var answered = Answered(method);
        var nullableItems = IsNullableCollection(answered);
        foreach (var (status, response) in operation.Responses)
        {
            if (response is not OpenApiResponse { Content: { } content })
                continue;

            // the type the response is declared with: the action's answer, or a ProducesResponseType's
            var declared = int.TryParse(status, out var statusCode)
                ? description.SupportedResponseTypes.FirstOrDefault(x => x.StatusCode == statusCode)?.Type
                : null;

            var isValue = (declared ?? answered.Type).IsValueType;

            foreach (var mediaType in content.Values.OfType<OpenApiMediaType>())
            {
                // an object, a collection or a string is answered never null, as NSwag's document had it whatever the
                // action declares; since .NET 11 the framework puts the null of a nullable one around its schema
                if (!isValue)
                {
                    if (mediaType.Schema is OpenApiSchema
                        { OneOf: [OpenApiSchema { Type: JsonSchemaType.Null }, var value] })
                        mediaType.Schema = value;

                    if (mediaType.Schema is OpenApiSchema { Type: { } schemaType } schema &&
                        schemaType.HasFlag(JsonSchemaType.Null))
                        schema.Type = schemaType & ~JsonSchemaType.Null;
                }

                // a nullable value is the value's schema, nullable, without the oneOf the framework puts around it,
                // which a client would take for a type of its own
                else if (mediaType.Schema is OpenApiSchema
                         {
                             OneOf: [OpenApiSchema { Type: JsonSchemaType.Null }, OpenApiSchema { Type: { } valueType } value]
                         })
                {
                    value.Type = valueType | JsonSchemaType.Null;
                    mediaType.Schema = value;
                }

                // NSwag read a nullable collection's items as nullable too
                if (nullableItems && mediaType.Schema is OpenApiSchema { Items: OpenApiSchemaReference items } array)
                    array.Items = new OpenApiSchema
                        { OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, items] };
            }
        }
    }

    // the type an action answers, out of the Task, ValueTask or ActionResult around it, and whether it may be null
    private static NullabilityInfo Answered(MethodInfo method)
    {
        var answered = new NullabilityInfoContext().Create(method.ReturnParameter);
        while (answered.Type.IsGenericType && answered.Type.GetGenericTypeDefinition() is var definition &&
               (definition == typeof(Task<>) || definition == typeof(ValueTask<>) ||
                definition == typeof(ActionResult<>)))
            answered = answered.GenericTypeArguments[0];

        return answered;
    }

    // a collection of objects the action may answer null for, such as Task<Item[]?>
    private static bool IsNullableCollection(NullabilityInfo answered)
    {
        var type = answered.Type;
        var elementType = type.IsArray
            ? type.GetElementType()
            : type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type) &&
              type.GenericTypeArguments.Length == 1
                ? type.GenericTypeArguments[0]
                : null;

        return answered.ReadState == NullabilityState.Nullable && elementType is { IsValueType: false } &&
               elementType != typeof(string);
    }

    private static void TransformBody(OpenApiOperation operation, ApiParameterDescription description,
        ParameterInfo? parameterInfo, int position)
    {
        if (operation.RequestBody is not OpenApiRequestBody requestBody)
            return;

        requestBody.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        requestBody.Extensions["x-name"] = new JsonNodeExtension(JsonValue.Create(description.Name));
        requestBody.Extensions[PositionKey] = new JsonNodeExtension(JsonValue.Create(position));
        if (parameterInfo is { HasDefaultValue: false })
            requestBody.Required = true;

        // a body a client may leave out is not required, not nullable, as NSwag had it; a client would also take null
        foreach (var mediaType in requestBody.Content?.Values.OfType<OpenApiMediaType>() ?? [])
        {
            switch (mediaType.Schema)
            {
                case OpenApiSchema { OneOf: [OpenApiSchema { Type: JsonSchemaType.Null }, var value] }:
                    mediaType.Schema = value;
                    break;
                case OpenApiSchema { Type: { } type } schema:
                    schema.Type = type & ~JsonSchemaType.Null;
                    break;
            }
        }
    }

    private static void TransformParameter(OpenApiParameter parameter, ApiParameterDescription description,
        ParameterInfo? parameterInfo, int position)
    {
        parameter.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        parameter.Extensions[PositionKey] = new JsonNodeExtension(JsonValue.Create(position));

        // a model's property is required by its own metadata; only a parameter of the action is required for having
        // no default value
        if (parameterInfo != null && description.ModelMetadata.MetadataKind == ModelMetadataKind.Parameter)
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
                switch (parameter.Schema)
                {
                    case OpenApiSchema { Type: { } schemaType } nullableSchema:
                        nullableSchema.Type = schemaType | JsonSchemaType.Null;
                        break;
                    case OpenApiSchemaReference reference:
                        parameter.Schema = new OpenApiSchema
                        {
                            OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, reference]
                        };

                        break;
                }
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

        response.Content ??= new Dictionary<string, IOpenApiMediaType>();
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

        // any result an action writes itself, as a ContentResult or a RedirectResult, but not an ActionResult<T>
        return typeof(IActionResult).IsAssignableFrom(returnType);
    }
}
