using System.Security.Cryptography.X509Certificates;
using GrayMint.Common.Acme;
using Microsoft.AspNetCore.Http;

namespace GrayMint.Common.Test;

[TestClass]
public class AcmeCertificateIssuerTest
{
    private const string Domain = "checkout.example.com";

    private static AcmeIssueRequest CreateRequest(List<string> events, Func<HttpMessageHandler>? handlerFactory = null)
    {
        return new AcmeIssueRequest
        {
            AccountKeyPem = "account",
            Domain = Domain,
            PublishChallenge = (challenge, _) =>
            {
                events.Add($"publish:{challenge.Token}");
                return Task.CompletedTask;
            },
            WithdrawChallenge = (challenge, _) =>
            {
                events.Add($"withdraw:{challenge.Token}");
                return Task.CompletedTask;
            },
            SelfCheckHandlerFactory = handlerFactory,
            PollInterval = TimeSpan.FromMilliseconds(10)
        };
    }

    [TestMethod]
    public async Task Certificate_after_the_challenge_is_published_and_checked()
    {
        var events = new List<string>();
        var acmeClient = new TestAcmeClient(domain => new TestAcmeOrder(domain, pendingCount: 2));

        // port 80 answers what was published
        var (app, port) = await AcmeTestUtil.StartWebServer(context =>
        {
            var order = acmeClient.Orders.Single();
            events.Add("self-check");
            return context.Response.WriteAsync(order.KeyAuthorization);
        });
        await using var _ = app;

        var issuer = new AcmeCertificateIssuer(acmeClient);
        var pfx = await issuer.Issue(CreateRequest(events, () => AcmeTestUtil.CreateLoopbackHandler(port)));

        var order = acmeClient.Orders.Single();
        CollectionAssert.AreEqual(new[] { $"publish:{order.Token}", "self-check", $"withdraw:{order.Token}" }, events);
        Assert.AreEqual(3, order.ValidateCount, "pending twice, then signed");
        using var certificate = X509CertificateLoader.LoadPkcs12(pfx, password: null);
        Assert.AreEqual($"CN={Domain}", certificate.Subject);
    }

    [TestMethod]
    public async Task Failed_self_check_asks_the_authority_nothing_and_withdraws()
    {
        var events = new List<string>();
        var acmeClient = new TestAcmeClient(domain => new TestAcmeOrder(domain));
        var (app, port) = await AcmeTestUtil.StartWebServer(context => context.Response.WriteAsync("another server"));
        await using var _ = app;

        var issuer = new AcmeCertificateIssuer(acmeClient);
        var ex = await Assert.ThrowsExactlyAsync<AcmeSelfCheckException>(() =>
            issuer.Issue(CreateRequest(events, () => AcmeTestUtil.CreateLoopbackHandler(port))));

        var order = acmeClient.Orders.Single();
        Assert.AreEqual(AcmeSelfCheckError.OtherAnswer, ex.Error);
        Assert.AreEqual(0, order.ValidateCount);
        CollectionAssert.AreEqual(new[] { $"publish:{order.Token}", $"withdraw:{order.Token}" }, events);
    }

    [TestMethod]
    public async Task Refused_challenge_withdraws()
    {
        var events = new List<string>();
        var acmeClient = new TestAcmeClient(domain =>
            new TestAcmeOrder(domain, failure: new AcmeException("The challenge is invalid.")));

        var issuer = new AcmeCertificateIssuer(acmeClient);
        await Assert.ThrowsExactlyAsync<AcmeException>(() => issuer.Issue(CreateRequest(events)));

        var order = acmeClient.Orders.Single();
        CollectionAssert.AreEqual(new[] { $"publish:{order.Token}", $"withdraw:{order.Token}" }, events);
    }

    [TestMethod]
    public async Task Authority_that_does_not_decide_in_time_fails()
    {
        var events = new List<string>();
        var acmeClient = new TestAcmeClient(domain => new TestAcmeOrder(domain, pendingCount: int.MaxValue));
        var request = CreateRequest(events);

        var issuer = new AcmeCertificateIssuer(acmeClient);
        var ex = await Assert.ThrowsExactlyAsync<AcmeTimeoutException>(() => issuer.Issue(new AcmeIssueRequest
        {
            AccountKeyPem = request.AccountKeyPem,
            Domain = request.Domain,
            PublishChallenge = request.PublishChallenge,
            WithdrawChallenge = request.WithdrawChallenge,
            PollInterval = TimeSpan.FromMilliseconds(10),
            ValidateTimeout = TimeSpan.FromMilliseconds(200)
        }));

        StringAssert.Contains(ex.Message, "did not check");
        Assert.AreEqual(2, events.Count, "published and withdrawn");
    }

    [TestMethod]
    public async Task Publish_that_kept_the_challenge_and_then_failed_still_withdraws()
    {
        var events = new List<string>();
        var acmeClient = new TestAcmeClient(domain => new TestAcmeOrder(domain));
        var request = CreateRequest(events);

        var issuer = new AcmeCertificateIssuer(acmeClient);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => issuer.Issue(new AcmeIssueRequest
        {
            AccountKeyPem = request.AccountKeyPem,
            Domain = request.Domain,
            PublishChallenge = async (challenge, ct) =>
            {
                await request.PublishChallenge(challenge, ct);
                throw new InvalidOperationException("The save failed after the commit.");
            },
            WithdrawChallenge = request.WithdrawChallenge
        }));

        var order = acmeClient.Orders.Single();
        CollectionAssert.AreEqual(new[] { $"publish:{order.Token}", $"withdraw:{order.Token}" }, events);
        Assert.AreEqual(0, order.ValidateCount);
    }

    [TestMethod]
    public async Task Cancellation_withdraws_and_stays_a_cancellation()
    {
        var events = new List<string>();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var acmeClient = new TestAcmeClient(domain => new TestAcmeOrder(domain, pendingCount: int.MaxValue));

        var issuer = new AcmeCertificateIssuer(acmeClient);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            issuer.Issue(CreateRequest(events), cancellationTokenSource.Token));

        Assert.AreEqual(2, events.Count, "published and withdrawn");
    }
}
