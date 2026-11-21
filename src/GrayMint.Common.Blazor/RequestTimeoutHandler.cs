namespace GrayMint.Common.Blazor;

// HttpClient ends a request that outlives its Timeout as a cancellation, and Blazor drops a canceled
// event handler's exception, so a card's refresh that timed out failed without a word. This handler
// owns the timeout and fails such a request with a TimeoutException, which the card reports. Its
// HttpClient needs an infinite Timeout, or the HttpClient's own fires first.
public class RequestTimeoutHandler : DelegatingHandler
{
    // HttpClient's default
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(100);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);
        try
        {
            return await base.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException ex)
        {
            // the caller's own cancellation stays one
            if (cancellationToken.IsCancellationRequested)
                throw;

            throw new TimeoutException(
                $"The server did not answer within {RequestTimeout.TotalSeconds} seconds.", ex);
        }
    }
}
