namespace GrayMint.Common.Acme;

public class AcmeClientOptions
{
    /// <summary>The authority's directory; Let's Encrypt by default.</summary>
    public Uri DirectoryUri { get; set; } = AcmeDirectories.LetsEncrypt;

    /// <summary>
    /// The client every request to the authority goes through, such as one through a proxy, or one that trusts a test
    /// authority's own certificate; <see langword="null"/> for one of Certes'. It is the caller's, and not disposed here.
    /// </summary>
    public HttpClient? HttpClient { get; set; }
}
