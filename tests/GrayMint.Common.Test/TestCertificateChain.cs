namespace GrayMint.Common.Test;

internal sealed class TestCertificateChain
{
    public required string CertificatePem { get; init; }
    public required string IssuerPem { get; init; }
    public required string KeyPem { get; init; }
}
