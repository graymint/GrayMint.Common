using System.Net.Http.Headers;
using System.Text;

// ReSharper disable CollectionNeverUpdated.Global

namespace GrayMint.Common.ApiClients;

public abstract class ApiClientCommon
{
    public AuthenticationHeaderValue? DefaultAuthorization { get; set; }
    public bool ReadResponseAsString { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();

    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder,
        CancellationToken cancellationToken)
    {
        return PrepareRequestAsync(client, request, urlBuilder.ToString(), cancellationToken);
    }

    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url,
        CancellationToken cancellationToken)
    {
        _ = url;
        _ = client;
        _ = cancellationToken;

        // build url
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        // add default headers
        foreach (var header in DefaultHeaders)
            request.Headers.Add(header.Key, header.Value);

        // add authorization header
        if (DefaultAuthorization != null && request.Headers.Authorization == null)
            request.Headers.Authorization = DefaultAuthorization;

        return Task.CompletedTask;
    }

    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        _ = client;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
