namespace GrayMint.Common.Acme;

/// <summary>
/// Gives the certificate of a domain, as PFX bytes with no password (its key, the certificate and its intermediates),
/// or <see langword="null"/> when the domain has none. The domain is the name the client asked for, lower-cased,
/// without a trailing dot. Every connection to a name not in the cache calls it, whatever name the client sends, so a
/// name the app does not serve should be refused without a database read.
/// </summary>
public delegate ValueTask<byte[]?> TlsCertificateLoader(string domain, CancellationToken cancellationToken);
