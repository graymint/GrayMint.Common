namespace GrayMint.Common.Acme;

/// <summary>
/// Reads a challenge's URL the way the authority will, before the authority is asked to: a domain that does not point
/// to the server yet fails here, and spends none of the authority's limit on failed validations (Let's Encrypt allows
/// five an hour for a domain).
/// </summary>
public static class AcmeSelfCheck
{
    public const string PathPrefix = "/.well-known/acme-challenge/";
    private const int MaxAnswerLength = 4096;

    public static string ChallengeUrl(string domain, string token) => $"http://{domain}{PathPrefix}{token}";

    /// <summary>
    /// Reads <c>http://domain/.well-known/acme-challenge/token</c> through the handler and compares it with the key
    /// authorization. The domain is the user's, so the handler should reach public addresses only and follow no
    /// redirect; it is not disposed here.
    /// </summary>
    /// <exception cref="AcmeSelfCheckException">The URL did not answer with the key authorization.</exception>
    public static async Task Check(string domain, string token, string keyAuthorization, HttpMessageHandler handler,
        TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        httpClient.Timeout = timeout;
        httpClient.MaxResponseContentBufferSize = MaxAnswerLength;

        string answer;
        try
        {
            answer = await httpClient.GetStringAsync(ChallengeUrl(domain, token), cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.HttpRequestError is HttpRequestError.NameResolutionError)
        {
            throw new AcmeSelfCheckException(AcmeSelfCheckError.NoAddress,
                $"{domain} has no public IP address. Point it to the server's public IP address.", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode != null || ex.HttpRequestError is
                                                  HttpRequestError.InvalidResponse or HttpRequestError.HttpProtocolError or
                                                  HttpRequestError.ConfigurationLimitExceeded)
        {
            throw OtherAnswer(domain, ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException &&
                                   !cancellationToken.IsCancellationRequested)
        {
            throw new AcmeSelfCheckException(AcmeSelfCheckError.NotReached,
                $"Port 80 of {domain} could not be reached. Open port 80 to the server.", ex);
        }

        // the authority, too, ignores the whitespace at the end (RFC 8555, 8.3)
        if (answer.TrimEnd() != keyAuthorization)
            throw OtherAnswer(domain, null);
    }

    private static AcmeSelfCheckException OtherAnswer(string domain, Exception? innerException) =>
        new(AcmeSelfCheckError.OtherAnswer,
            $"Something other than the server answered on port 80 of {domain}. Make sure port 80 goes to the server.",
            innerException);
}
