namespace GrayMint.Common.Acme;

/// <summary>
/// Gives the key authorization of a challenge's token on its domain, or <see langword="null"/> when there is no such
/// challenge. The domain is the request's host, lower-cased, without its port.
/// </summary>
public delegate ValueTask<string?> AcmeKeyAuthorizationProvider(string domain, string token,
    CancellationToken cancellationToken);
