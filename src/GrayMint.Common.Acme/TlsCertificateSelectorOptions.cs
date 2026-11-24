namespace GrayMint.Common.Acme;

public class TlsCertificateSelectorOptions
{
    /// <summary>How long a loaded certificate is used before it is loaded again, which takes a renewed one.</summary>
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long a domain no connection names stays in the cache, so one gone for good is not kept; a domain in use keeps
    /// the certificate loaded before, for a load that fails.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The domain whose certificate a connection gets when it names no domain, or one with no certificate;
    /// <see langword="null"/> refuses such a connection.
    /// </summary>
    public string? DefaultDomain { get; set; }
}
