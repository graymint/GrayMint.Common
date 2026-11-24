using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using GrayMint.Common.Acme;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GrayMint.Common.Test;

[TestClass]
public class TlsCertificateSelectorTest : TestBase
{
    private const string Domain1 = "checkout.one.example";
    private const string Domain2 = "checkout.two.example";
    private const string DefaultDomain = "checkout.default.example";

    private static readonly Dictionary<string, byte[]> DomainCertificates = new()
    {
        [Domain1] = AcmeTestUtil.CreatePfx(Domain1),
        [Domain2] = AcmeTestUtil.CreatePfx(Domain2),
        [DefaultDomain] = AcmeTestUtil.CreatePfx(DefaultDomain)
    };

    private sealed class Loader
    {
        public List<string> Loaded { get; } = [];
        public Dictionary<string, byte[]> Certificates { get; } = new(DomainCertificates);

        public ValueTask<byte[]?> Load(string domain, CancellationToken cancellationToken)
        {
            lock (Loaded)
                Loaded.Add(domain);
            return ValueTask.FromResult(Certificates.GetValueOrDefault(domain));
        }
    }

    private Task<WebApplication> StartServer(TlsCertificateSelector selector)
    {
        return AcmeTestUtil.StartWebServer(
            context => context.Response.WriteAsync(context.Request.Host.Host, context.RequestAborted),
            listenOptions => listenOptions.UseHttps(selector));
    }

    // the subject of the certificate the server sent for the name
    private async Task<string> Connect(int port, string? serverName)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await using var sslStream = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            // an IP address as the target host sends no name, as a client that names none
            TargetHost = serverName ?? IPAddress.Loopback.ToString()
        });

        return sslStream.RemoteCertificate?.Subject
               ?? throw new AssertFailedException("The server sent no certificate.");
    }

    [TestMethod]
    public async Task Each_domain_gets_its_certificate()
    {
        var loader = new Loader();
        await using var app = await StartServer(new TlsCertificateSelector(loader.Load));
        var port = AcmeTestUtil.GetPort(app);

        Assert.AreEqual($"CN={Domain1}", await Connect(port, Domain1));
        Assert.AreEqual($"CN={Domain2}", await Connect(port, Domain2.ToUpperInvariant()));
    }

    [TestMethod]
    public async Task Request_is_served_over_the_selected_certificate()
    {
        await using var app = await StartServer(new TlsCertificateSelector(new Loader().Load));

        using var handler = new SocketsHttpHandler();
        handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
            certificate?.Subject == $"CN={Domain1}";

        handler.ConnectCallback = AcmeTestUtil.CreateLoopbackHandler(AcmeTestUtil.GetPort(app)).ConnectCallback;

        // a client of this test's own, for its check of the certificate
        // ReSharper disable once ShortLivedHttpClient
        using var httpClient = new HttpClient(handler);
        Assert.AreEqual(Domain1, await httpClient.GetStringAsync($"https://{Domain1}/"));
    }

    [TestMethod]
    public async Task Certificate_is_loaded_once_within_the_cache_time()
    {
        var loader = new Loader();
        await using var app = await StartServer(new TlsCertificateSelector(loader.Load));
        var port = AcmeTestUtil.GetPort(app);

        await Connect(port, Domain1);
        await Connect(port, Domain1);
        Assert.AreSequenceEqual([Domain1], loader.Loaded);
    }

    [TestMethod]
    public async Task Renewed_certificate_is_taken_after_the_cache_time_or_an_invalidation()
    {
        var loader = new Loader();
        var selector = new TlsCertificateSelector(loader.Load);
        await using var app = await StartServer(selector);
        var port = AcmeTestUtil.GetPort(app);

        Assert.AreEqual($"CN={Domain1}", await Connect(port, Domain1));
        loader.Certificates[Domain1] = AcmeTestUtil.CreatePfx("renewed.one.example");
        Assert.AreEqual($"CN={Domain1}", await Connect(port, Domain1), "cached");

        selector.Invalidate(Domain1);
        Assert.AreEqual("CN=renewed.one.example", await Connect(port, Domain1));

        var expiringSelector = new TlsCertificateSelector(loader.Load,
            new TlsCertificateSelectorOptions { CacheTimeout = TimeSpan.Zero });

        var options1 = await expiringSelector.GetServerOptions(Domain2);
        var options2 = await expiringSelector.GetServerOptions(Domain2);
        Assert.AreNotSame(options1.ServerCertificateContext, options2.ServerCertificateContext);
    }

    [TestMethod]
    public async Task Unknown_or_no_name_gets_the_default_domain_certificate()
    {
        await using var app = await StartServer(new TlsCertificateSelector(new Loader().Load,
            new TlsCertificateSelectorOptions { DefaultDomain = DefaultDomain }));

        var port = AcmeTestUtil.GetPort(app);

        Assert.AreEqual($"CN={DefaultDomain}", await Connect(port, "unknown.example"));
        Assert.AreEqual($"CN={DefaultDomain}", await Connect(port, null));
    }

    [TestMethod]
    public async Task Unknown_name_is_refused_without_a_default()
    {
        await using var app = await StartServer(new TlsCertificateSelector(new Loader().Load));
        var port = AcmeTestUtil.GetPort(app);

        await Assert.ThrowsAsync<Exception>(() => Connect(port, "unknown.example"));
        await Assert.ThrowsExactlyAsync<AuthenticationException>(async () =>
            await new TlsCertificateSelector(new Loader().Load).GetServerOptions("unknown.example"));
    }

    [TestMethod]
    public async Task Failed_load_keeps_the_certificate_it_had()
    {
        // the test sets it to make the next load fail
        var fail = false;
        // ReSharper disable once AccessToModifiedClosure
        var selector = new TlsCertificateSelector((domain, _) => fail
                ? throw new InvalidOperationException("The database is down.")
                : ValueTask.FromResult<byte[]?>(DomainCertificates[domain]),
            new TlsCertificateSelectorOptions { CacheTimeout = TimeSpan.Zero });

        var options1 = await selector.GetServerOptions(Domain1);
        fail = true;
        var options2 = await selector.GetServerOptions(Domain1);
        Assert.AreSame(options1.ServerCertificateContext, options2.ServerCertificateContext);
    }

    [TestMethod]
    public async Task Pfx_that_will_not_load_keeps_the_certificate_it_had()
    {
        // the test sets it to make the next load give a PFX that will not load
        var corrupt = false;
        var selector = new TlsCertificateSelector((domain, _) =>
                // ReSharper disable once AccessToModifiedClosure
                ValueTask.FromResult<byte[]?>(corrupt ? [1, 2, 3] : DomainCertificates[domain]),
            new TlsCertificateSelectorOptions { CacheTimeout = TimeSpan.Zero });

        var options1 = await selector.GetServerOptions(Domain1);
        corrupt = true;
        var options2 = await selector.GetServerOptions(Domain1);
        Assert.AreSame(options1.ServerCertificateContext, options2.ServerCertificateContext);

        // with none loaded before, the connection is refused, as with no certificate at all
        await Assert.ThrowsAsync<Exception>(async () => await selector.GetServerOptions(Domain2));
    }

    [TestMethod]
    public async Task Domain_no_connection_names_is_dropped_after_the_idle_time()
    {
        var loader = new Loader();
        var time = new TestTimeProvider();
        var selector = new TlsCertificateSelector(loader.Load, new TlsCertificateSelectorOptions
        {
            CacheTimeout = TimeSpan.FromMinutes(10),
            IdleTimeout = TimeSpan.FromMinutes(20)
        }, timeProvider: time);

        await selector.GetServerOptions(Domain1);
        time.Advance(TimeSpan.FromMinutes(15));
        await selector.GetServerOptions(Domain2); // no sweep yet: the last one was 15 minutes ago
        time.Advance(TimeSpan.FromMinutes(10));
        loader.Certificates.Remove(Domain1);

        // a sweep: Domain1 has been idle 25 minutes and goes, Domain2 10
        await selector.GetServerOptions(Domain2);

        // gone from the cache, it is loaded again, and the store has no certificate for it any more
        await Assert.ThrowsAsync<Exception>(async () => await selector.GetServerOptions(Domain1));
    }

    [TestMethod]
    public void Pfx_chain_is_sent_with_the_certificate()
    {
        var chain = AcmeTestUtil.CreateChain(Domain1);
        var pfx = AcmeOrder.ToPfx(chain.CertificatePem, [chain.IssuerPem], chain.KeyPem);
        var context = TlsCertificateSelector.CreateCertificateContext(pfx);

        Assert.AreEqual($"CN={Domain1}", context.TargetCertificate.Subject);
        Assert.IsTrue(context.TargetCertificate.HasPrivateKey);
        Assert.AreEqual("CN=Test Intermediate", context.IntermediateCertificates.Single().Subject);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public void Advance(TimeSpan time) => _now += time;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
