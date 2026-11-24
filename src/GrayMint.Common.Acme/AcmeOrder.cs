using System.Security.Cryptography.X509Certificates;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

namespace GrayMint.Common.Acme;

public class AcmeOrder(IOrderContext orderContext, IChallengeContext challengeContext, string domain) : IAcmeOrder
{
    private bool _validateRequested;
    private byte[]? _pfx;

    public string Domain => domain;
    public string Token => challengeContext.Token;
    public string KeyAuthorization => challengeContext.KeyAuthz;

    public async Task<byte[]?> Validate(CancellationToken cancellationToken = default)
    {
        // the order is finalized once: another call gives the same certificate, not another one with a new key
        if (_pfx != null)
            return _pfx;

        // asking again would only ask the same; RFC 8555 has the client read the challenge while it is being checked
        var challenge = _validateRequested
            ? await challengeContext.Resource().WaitAsync(cancellationToken)
            : await challengeContext.Validate().WaitAsync(cancellationToken);

        _validateRequested = true;

        return challenge.Status switch
        {
            ChallengeStatus.Valid => _pfx = await GenerateCertificate(cancellationToken),
            ChallengeStatus.Invalid => throw new AcmeException(
                $"The certificate authority found the challenge of {domain} invalid. {challenge.Error?.Detail}".TrimEnd()),
            _ => null
        };
    }

    private async Task<byte[]> GenerateCertificate(CancellationToken cancellationToken)
    {
        var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

        // The name alone, as Let's Encrypt signs nothing else. Add no other field: CsrInfo keeps one set to null, and
        // Certes refuses the empty Country it makes of it.
        var chain = await orderContext.Generate(new CsrInfo { CommonName = domain }, privateKey)
            .WaitAsync(cancellationToken);

        return ToPfx(chain.Certificate.ToPem(), chain.Issuers.Select(x => x.ToPem()), privateKey.ToPem());
    }

    // .NET, not Certes' PfxBuilder: the builder looks for a root it knows, so a new root of the authority's would stop
    // every order; a server sends the intermediates, and a client has its own roots
    internal static byte[] ToPfx(string certificatePem, IEnumerable<string> issuerPems, string privateKeyPem)
    {
        var certificates = new X509Certificate2Collection { X509Certificate2.CreateFromPem(certificatePem, privateKeyPem) };
        try
        {
            foreach (var issuerPem in issuerPems)
                certificates.Add(X509Certificate2.CreateFromPem(issuerPem));

            return certificates.Export(X509ContentType.Pkcs12)
                   ?? throw new InvalidOperationException("The certificate could not be exported.");
        }
        finally
        {
            foreach (var certificate in certificates)
                certificate.Dispose();
        }
    }
}
