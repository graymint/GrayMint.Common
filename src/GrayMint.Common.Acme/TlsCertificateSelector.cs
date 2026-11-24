using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.Acme;

/// <summary>
/// The certificate of each TLS connection, by the domain the client names (SNI), for Kestrel: see
/// <see cref="TlsCertificateSelectorExtensions.UseHttps(ListenOptions, TlsCertificateSelector)"/>.
/// </summary>
public class TlsCertificateSelector(
    TlsCertificateLoader loader,
    TlsCertificateSelectorOptions? options = null,
    ILogger<TlsCertificateSelector>? logger = null,
    TimeProvider? timeProvider = null)
{
    private readonly TlsCertificateSelectorOptions _options = options ?? new TlsCertificateSelectorOptions();
    private readonly ILogger _logger = logger ?? NullLogger<TlsCertificateSelector>.Instance;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private DateTime _sweptTime = DateTime.MinValue;

    private sealed class Entry(SslStreamCertificateContext context, DateTime loadedTime)
    {
        public SslStreamCertificateContext Context { get; } = context;
        public DateTime LoadedTime { get; } = loadedTime;
        public DateTime UsedTime { get; set; } = loadedTime;
    }

    /// <summary>The TLS options of a connection to the domain.</summary>
    /// <exception cref="AuthenticationException">Neither the domain nor the default domain has a certificate.</exception>
    public async ValueTask<SslServerAuthenticationOptions> GetServerOptions(string? serverName,
        CancellationToken cancellationToken = default)
    {
        var domain = Normalize(serverName);
        var context = domain != null ? await GetCertificateContext(domain, cancellationToken) : null;
        if (context == null && Normalize(_options.DefaultDomain) is { } defaultDomain && domain != defaultDomain)
            context = await GetCertificateContext(defaultDomain, cancellationToken);

        // new options each time, as Kestrel sets the connection's protocols on them
        return context != null
            ? new SslServerAuthenticationOptions { ServerCertificateContext = context }
            : throw new AuthenticationException($"There is no certificate for {serverName}.");
    }

    /// <summary>Drops the domain's certificate from the cache, so the next connection loads it again.</summary>
    public void Invalidate(string domain)
    {
        var normalized = Normalize(domain);
        if (normalized != null)
            _entries.TryRemove(normalized, out _);
    }

    private async ValueTask<SslStreamCertificateContext?> GetCertificateContext(string domain,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        Sweep(now);
        if (_entries.TryGetValue(domain, out var entry)) {
            entry.UsedTime = now;
            if (now - entry.LoadedTime < _options.CacheTimeout)
                return entry.Context;
        }

        try
        {
            return await LoadCertificateContext(domain, now, cancellationToken);
        }
        catch (Exception ex) when (entry != null && !cancellationToken.IsCancellationRequested)
        {
            // Neither a failed load nor a PFX that won't load takes the connection down: the one loaded before serves
            // it, which does no worse than a refused connection even if it has expired since.
            _logger.LogWarning(ex, "Could not load a certificate; the one loaded before is used. Domain: {Domain}", domain);
            return entry.Context;
        }
    }

    private async ValueTask<SslStreamCertificateContext?> LoadCertificateContext(string domain, DateTime now,
        CancellationToken cancellationToken)
    {
        var pfx = await loader(domain, cancellationToken);
        if (pfx == null)
        {
            _entries.TryRemove(domain, out _);
            return null;
        }

        var context = CreateCertificateContext(pfx);
        _entries[domain] = new Entry(context, now);
        return context;
    }

    // at most once an idle time, so a connection seldom pays for it
    private void Sweep(DateTime now)
    {
        if (now - _sweptTime < _options.IdleTimeout)
            return;

        _sweptTime = now;
        foreach (var pair in _entries)
            if (now - pair.Value.UsedTime >= _options.IdleTimeout)
                _entries.TryRemove(pair);
    }

    /// <summary>The certificate with the key, sent with the other certificates of the PFX as its chain.</summary>
    public static SslStreamCertificateContext CreateCertificateContext(byte[] pfx)
    {
        var certificates = X509CertificateLoader.LoadPkcs12Collection(pfx, password: null);
        var certificate = certificates.FirstOrDefault(x => x.HasPrivateKey)
                          ?? throw new InvalidOperationException("The PFX has no certificate with a private key.");

        var intermediates =
            new X509Certificate2Collection(certificates.Where(x => !ReferenceEquals(x, certificate)).ToArray());

        return SslStreamCertificateContext.Create(certificate, intermediates, offline: true);
    }

    private static string? Normalize(string? domain)
    {
        domain = domain?.Trim().TrimEnd('.').ToLowerInvariant();
        return string.IsNullOrEmpty(domain) ? null : domain;
    }
}
