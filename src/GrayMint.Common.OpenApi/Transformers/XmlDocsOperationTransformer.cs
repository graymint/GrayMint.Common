using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// An action's doc comments: its summary, its remarks, its parameters, its body and what it returns. ASP.NET Core's
// own reader of them works only for an AddOpenApi call in the app's project, and AddGrayMintOpenApi makes that call.
// A word an app's own transformer gave first is kept.
internal sealed class XmlDocsOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        var method = action.MethodInfo;
        operation.Summary ??= XmlDocs.Summary(method);
        operation.Description ??= XmlDocs.Remarks(method);

        var parameters = method.GetParameters();
        foreach (var parameter in (operation.Parameters ?? []).OfType<OpenApiParameter>())
        {
            var parameterInfo = parameters.FirstOrDefault(x =>
                string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

            parameter.Description ??= XmlDocs.Parameter(parameterInfo);
        }

        if (operation.RequestBody is OpenApiRequestBody requestBody)
        {
            var bodyParameter = context.Description.ParameterDescriptions
                .FirstOrDefault(x => x.Source == BindingSource.Body || x.Source == BindingSource.Form)
                ?.ParameterDescriptor as ControllerParameterDescriptor;

            requestBody.Description ??= XmlDocs.Parameter(bodyParameter?.ParameterInfo);
        }

        if (XmlDocs.Returns(method) is { } returns &&
            operation.Responses?.TryGetValue("200", out var response) == true &&
            response is OpenApiResponse okResponse)
            okResponse.Description = returns;

        return Task.CompletedTask;
    }
}
