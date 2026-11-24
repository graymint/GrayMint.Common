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
