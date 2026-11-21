using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace GrayMint.Common.OpenApi.Transformers;

// A body's JSON under the other names the framework accepts and answers, which the document leaves out
// (ApiDocsDocumentTransformer), left out of the API's description before the framework makes the document from it: the
// framework makes a schema for each name of a body, and of a class with references back up that is tens of thousands of
// schemas, three times over, all but one to be thrown away.
internal sealed class JsonAliasApiDescriptionProvider : IApiDescriptionProvider
{
    // after the framework's own, which describe the actions and endpoints
    public int Order => 1000;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        foreach (var description in context.Results)
        {
            RemoveAliases(description.SupportedRequestFormats, x => x.MediaType);
            foreach (var responseType in description.SupportedResponseTypes)
                RemoveAliases(responseType.ApiResponseFormats, x => x.MediaType);
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    private static void RemoveAliases<TFormat>(IList<TFormat> formats, Func<TFormat, string> mediaType)
    {
        if (!formats.Any(x => mediaType(x) == ApiDocsDocumentTransformer.JsonContentType))
            return;

        for (var i = formats.Count - 1; i >= 0; i--)
            if (ApiDocsDocumentTransformer.JsonAliases.Contains(mediaType(formats[i])))
                formats.RemoveAt(i);
    }
}
