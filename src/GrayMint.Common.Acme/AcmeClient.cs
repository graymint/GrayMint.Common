using Certes;
using Certes.Acme;

namespace GrayMint.Common.Acme;

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
        await acmeContext.NewAccount(contact: null, termsOfServiceAgreed: true).WaitAsync(cancellationToken);
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
        return new AcmeContext(_directoryUri, accountKey, http);
    }
}
