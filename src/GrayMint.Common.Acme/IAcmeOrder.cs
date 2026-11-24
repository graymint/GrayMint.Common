namespace GrayMint.Common.Acme;

/// <summary>A certificate ordered for one domain, waiting for its HTTP-01 challenge to be proven.</summary>
public interface IAcmeOrder
{
    string Domain { get; }

    /// <summary>The challenge's token: the last segment of <c>http://domain/.well-known/acme-challenge/token</c>.</summary>
    string Token { get; }

    /// <summary>What that URL must answer.</summary>
    string KeyAuthorization { get; }

    /// <summary>
    /// Asks the authority to check the challenge the first time, and how its check stands after that.
    /// </summary>
    /// <returns>
    /// The certificate once the challenge is valid, as PFX bytes with no password: its key, the certificate and the
    /// authority's intermediates; <see langword="null"/> while the authority is still checking.
    /// </returns>
    /// <exception cref="AcmeException">The authority found the challenge invalid.</exception>
    Task<byte[]?> Validate(CancellationToken cancellationToken = default);
}
