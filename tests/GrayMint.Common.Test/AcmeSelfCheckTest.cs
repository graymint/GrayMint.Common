using System.Net.Sockets;
using GrayMint.Common.Acme;
using Microsoft.AspNetCore.Http;

namespace GrayMint.Common.Test;

[TestClass]
public class AcmeSelfCheckTest
{
    private const string Domain = "checkout.example.com";
    private const string Token = "abc_DEF-123";
    private const string KeyAuthorization = "abc_DEF-123.thumbprint";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static async Task<AcmeSelfCheckException?> Check(RequestDelegate server)
    {
        var (app, port) = await AcmeTestUtil.StartWebServer(server);
        await using var _ = app;
        using var handler = AcmeTestUtil.CreateLoopbackHandler(port);
        try
        {
            await AcmeSelfCheck.Check(Domain, Token, KeyAuthorization, handler, Timeout);
            return null;
        }
        catch (AcmeSelfCheckException ex)
        {
            return ex;
        }
    }

    [TestMethod]
    public async Task Key_authorization_passes()
    {
        string? path = null;
        string? host = null;
        var ex = await Check(context =>
        {
            path = context.Request.Path;
            host = context.Request.Host.Value;
            return context.Response.WriteAsync(KeyAuthorization + "\n");
        });

        Assert.IsNull(ex);
        Assert.AreEqual(AcmeSelfCheck.PathPrefix + Token, path);
        Assert.AreEqual(Domain, host);
    }

    [TestMethod]
    public async Task Other_answer_fails()
    {
        var ex = await Check(context => context.Response.WriteAsync("hello"));
        Assert.AreEqual(AcmeSelfCheckError.OtherAnswer, ex?.Error);
        StringAssert.Contains(ex!.Message, Domain);
    }

    [TestMethod]
    public async Task Not_found_is_another_answer()
    {
        var ex = await Check(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        Assert.AreEqual(AcmeSelfCheckError.OtherAnswer, ex?.Error);
    }

    [TestMethod]
    public async Task Long_answer_is_another_answer()
    {
        var ex = await Check(context => context.Response.WriteAsync(new string('a', 10_000)));
        Assert.AreEqual(AcmeSelfCheckError.OtherAnswer, ex?.Error);
    }

    [TestMethod]
    public async Task Redirect_is_not_followed()
    {
        var ex = await Check(context =>
        {
            context.Response.Redirect("http://127.0.0.1:2019/config");
            return Task.CompletedTask;
        });
        Assert.AreEqual(AcmeSelfCheckError.OtherAnswer, ex?.Error);
    }

    [TestMethod]
    public async Task Closed_port_is_not_reached()
    {
        using var handler = AcmeTestUtil.CreateLoopbackHandler(AcmeTestUtil.GetFreePort());
        var ex = await Assert.ThrowsExactlyAsync<AcmeSelfCheckException>(() =>
            AcmeSelfCheck.Check(Domain, Token, KeyAuthorization, handler, Timeout));
        Assert.AreEqual(AcmeSelfCheckError.NotReached, ex.Error);
    }

    [TestMethod]
    public async Task Name_with_no_address_has_no_address()
    {
        // as a handler that connects to public addresses only reports a name with private ones
        using var handler = new SocketsHttpHandler
        {
            ConnectCallback = (_, _) => throw new SocketException((int)SocketError.HostNotFound)
        };
        var ex = await Assert.ThrowsExactlyAsync<AcmeSelfCheckException>(() =>
            AcmeSelfCheck.Check(Domain, Token, KeyAuthorization, handler, Timeout));
        Assert.AreEqual(AcmeSelfCheckError.NoAddress, ex.Error);
    }

    [TestMethod]
    public async Task Cancellation_is_not_a_failure_of_the_check()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var (app, port) = await AcmeTestUtil.StartWebServer(async context =>
        {
            await cancellationTokenSource.CancelAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), context.RequestAborted);
        });
        await using var _ = app;
        using var handler = AcmeTestUtil.CreateLoopbackHandler(port);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AcmeSelfCheck.Check(Domain, Token, KeyAuthorization, handler, Timeout, cancellationTokenSource.Token));
    }
}
