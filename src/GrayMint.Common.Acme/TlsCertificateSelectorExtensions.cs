using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace GrayMint.Common.Acme;

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
