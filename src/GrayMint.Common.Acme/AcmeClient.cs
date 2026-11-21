using Certes;
using Certes.Acme;

namespace GrayMint.Common.Acme;

/// <summary>An ACME certificate authority, such as Let's Encrypt: the seam a test replaces.</summary>
public interface IAcmeClient
{
    /// <summary>Opens a new account at the authority.</summary>
    /// <returns>The account's key, as PEM, which every later order of the account needs.</returns>
    Task<string> CreateAccount(CancellationToken cancellationToken = default);

    /// <summary>
    /// Orders a certificate for one domain, such as <c>checkout.example.com</c> (not a wildcard, nor an IP address), to
    /// be proven by an HTTP-01 challenge, on the account whose key <see cref="CreateAccount"/> gave.
    /// </summary>
    Task<IAcmeOrder> CreateOrder(string accountKeyPem, string domain, CancellationToken cancellationToken = default);
}

/// <summary>Where an <see cref="AcmeClient"/> orders its certificates.</summary>
public static class AcmeDirectories
{
    /// <summary>Let's Encrypt, whose certificates every browser trusts.</summary>
    public static Uri LetsEncrypt => WellKnownServers.LetsEncryptV2;

    /// <summary>Let's Encrypt's staging, with far higher limits and certificates nothing trusts: for trying a setup out.</summary>
    public static Uri LetsEncryptStaging => WellKnownServers.LetsEncryptStagingV2;
}

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

/// <summary>An ACME certificate authority, through Certes.</summary>
public class AcmeClient(AcmeClientOptions? options = null) : IAcmeClient
{
    private readonly Uri _directoryUri = options?.DirectoryUri ?? AcmeDirectories.LetsEncrypt;
    private readonly HttpClient? _httpClient = options?.HttpClient;

    // Certes' calls take no cancellation token: a cancelled wait leaves the call running, and the authority may still
    // make the account, the order or the certificate it was asked for
    public async Task<string> CreateAccount(CancellationToken cancellationToken = default)
    {
        // no contact: Let's Encrypt sends no expiry mail any more, nor keeps the addresses
        var acmeContext = CreateContext(null);
        await acmeContext.NewAccount(contact: null!, termsOfServiceAgreed: true).WaitAsync(cancellationToken);
        return acmeContext.AccountKey.ToPem();
    }

    public async Task<IAcmeOrder> CreateOrder(string accountKeyPem, string domain,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);

        var acmeContext = CreateContext(KeyFactory.FromPem(accountKeyPem));
        var orderContext = await acmeContext.NewOrder([domain]).WaitAsync(cancellationToken);
        var authorizations = await orderContext.Authorizations().WaitAsync(cancellationToken);
        var challengeContext = await authorizations.Single().Http().WaitAsync(cancellationToken);
        return new AcmeOrder(orderContext, challengeContext, domain);
    }

    private AcmeContext CreateContext(IKey? accountKey)
    {
        var http = _httpClient != null ? new AcmeHttpClient(_directoryUri, _httpClient) : null;
        return new AcmeContext(_directoryUri, accountKey!, http!);
    }
}
