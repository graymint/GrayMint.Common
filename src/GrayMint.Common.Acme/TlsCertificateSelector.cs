using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrayMint.Common.Acme;

/// <summary>
/// Gives the certificate of a domain, as PFX bytes with no password (its key, the certificate and its intermediates),
/// or <see langword="null"/> when the domain has none. The domain is the name the client asked for, lower-cased,
/// without a trailing dot. Every connection to a name not in the cache calls it, whatever name the client sends, so a
/// name the app does not serve should be refused without a database read.
/// </summary>
public delegate ValueTask<byte[]?> TlsCertificateLoader(string domain, CancellationToken cancellationToken);

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
        if (context == null && _options.DefaultDomain != null && domain != Normalize(_options.DefaultDomain))
            context = await GetCertificateContext(Normalize(_options.DefaultDomain)!, cancellationToken);

        if (context == null)
            throw new AuthenticationException($"There is no certificate for {serverName}.");

        // new options each time, as Kestrel sets the connection's protocols on them
        return new SslServerAuthenticationOptions { ServerCertificateContext = context };
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

        SslStreamCertificateContext? context;
        try
        {
            var pfx = await loader(domain, cancellationToken);
            context = pfx != null ? CreateCertificateContext(pfx) : null;
        }
        catch (Exception ex) when (entry != null && !cancellationToken.IsCancellationRequested)
        {
            // Neither a failed load nor a PFX that won't load takes the connection down: the one loaded before serves
            // it, which does no worse than a refused connection even if it has expired since.
            _logger.LogWarning(ex, "Could not load a certificate; the one loaded before is used. Domain: {Domain}", domain);
            return entry.Context;
        }

        if (context == null)
        {
            _entries.TryRemove(domain, out _);
            return null;
        }

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
        var intermediates = new X509Certificate2Collection(certificates.Where(x => x != certificate).ToArray());
        return SslStreamCertificateContext.Create(certificate, intermediates, offline: true);
    }

    private static string? Normalize(string? domain)
    {
        domain = domain?.Trim().TrimEnd('.').ToLowerInvariant();
        return string.IsNullOrEmpty(domain) ? null : domain;
    }
}

public static class TlsCertificateSelectorExtensions
{
    /// <summary>HTTPS on the listener, with the certificate of each connection from the selector.</summary>
    public static ListenOptions UseHttps(this ListenOptions listenOptions, TlsCertificateSelector selector)
    {
        return Microsoft.AspNetCore.Hosting.ListenOptionsHttpsExtensions.UseHttps(listenOptions, new TlsHandshakeCallbackOptions
        {
            OnConnection = context =>
                selector.GetServerOptions(context.ClientHelloInfo.ServerName, context.CancellationToken)
        });
    }
}
