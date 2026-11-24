using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.Test;

internal static class AcmeTestUtil
{
    /// <summary>A self-signed certificate of the domain with its key, as an authority's PFX is: no password.</summary>
    public static byte[] CreatePfx(string domain)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={domain}", key, HashAlgorithmName.SHA256);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(domain);
        request.CertificateExtensions.Add(san.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(90));
        return certificate.Export(X509ContentType.Pkcs12);
    }

    /// <summary>A certificate of the domain and the intermediate that signed it, each as PEM, as an authority sends them:
    /// the intermediate's root stays out.</summary>
    public static (string CertificatePem, string IssuerPem, string KeyPem) CreateChain(string domain)
    {
        // The intermediate is signed by a root, as an authority's is: a self-signed one is a root, which Linux rightly
        // leaves out of the chain a server sends.
        using var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var rootRequest = new CertificateRequest("CN=Test Root", rootKey, HashAlgorithmName.SHA256);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(730));

        using var caKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var caRequest = new CertificateRequest("CN=Test Intermediate", caKey, HashAlgorithmName.SHA256);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        using var caCertificate = caRequest.Create(root, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365),
            RandomNumberGenerator.GetBytes(16));
        using var ca = caCertificate.CopyWithPrivateKey(caKey);

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={domain}", key, HashAlgorithmName.SHA256);
        using var certificate = request.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(90),
            RandomNumberGenerator.GetBytes(16));
        return (certificate.ExportCertificatePem(), ca.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem());
    }

    /// <summary>Whatever name the URL has, the connection goes to the port on the loopback.</summary>
    public static SocketsHttpHandler CreateLoopbackHandler(int port)
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (_, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    /// <summary>A port on the loopback where nothing listens.</summary>
    public static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    /// <summary>Kestrel on a port of the loopback, answering with the request delegate.</summary>
    public static async Task<(WebApplication App, int Port)> StartWebServer(RequestDelegate requestDelegate,
        Action<ListenOptions>? configureListen = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listenOptions => configureListen?.Invoke(listenOptions)));
        var app = builder.Build();
        app.Run(requestDelegate);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return (app, new Uri(address).Port);
    }
}
