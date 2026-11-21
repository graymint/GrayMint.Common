using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Certes.Acme;
using Certes.Acme.Resource;
using GrayMint.Common.Acme;

namespace GrayMint.Common.Test;

// Certes itself, with no network: the order and the challenge are stand-ins that answer as an authority does
[TestClass]
public class AcmeOrderTest
{
    private const string Domain = "renew.example.com";

    [TestMethod]
    public async Task Request_has_only_the_name()
    {
        var orderContext = new TestOrderContext(Domain);
        var order = new AcmeOrder(orderContext, new TestChallengeContext(ChallengeStatus.Valid), Domain);

        // the test order stops at the download, once the request is built and sent
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => order.Validate());

        var request = orderContext.Request ?? throw new InvalidOperationException("No request has been sent.");
        var subject = CertificateRequest.LoadSigningRequest(request, HashAlgorithmName.SHA256).SubjectName.Name;
        Assert.AreEqual($"CN={Domain}", subject);
    }

    [TestMethod]
    public async Task Challenge_is_asked_once_then_read()
    {
        var challengeContext = new TestChallengeContext(ChallengeStatus.Pending, ChallengeStatus.Processing);
        var order = new AcmeOrder(new TestOrderContext(Domain), challengeContext, Domain);

        Assert.IsNull(await order.Validate());
        Assert.IsNull(await order.Validate());
        Assert.AreEqual(1, challengeContext.ValidateCount);
        Assert.AreEqual(1, challengeContext.ResourceCount);
    }

    [TestMethod]
    public async Task Invalid_challenge_says_the_authority_reason()
    {
        var challengeContext = new TestChallengeContext(ChallengeStatus.Invalid)
        {
            Error = new AcmeError { Detail = "Connection refused" }
        };
        var order = new AcmeOrder(new TestOrderContext(Domain), challengeContext, Domain);

        var ex = await Assert.ThrowsExactlyAsync<AcmeException>(() => order.Validate());
        StringAssert.Contains(ex.Message, Domain);
        StringAssert.Contains(ex.Message, "Connection refused");
    }

    [TestMethod]
    public async Task Signed_order_gives_the_key_the_certificate_and_its_issuer_once()
    {
        var orderContext = new SigningOrderContext(Domain);
        var order = new AcmeOrder(orderContext, new TestChallengeContext(ChallengeStatus.Valid), Domain);

        var pfx = await order.Validate();

        Assert.IsNotNull(pfx);
        var certificates = X509CertificateLoader.LoadPkcs12Collection(pfx, password: null);
        Assert.AreEqual(2, certificates.Count);
        Assert.AreEqual($"CN={Domain}", certificates.Single(x => x.HasPrivateKey).Subject);
        Assert.AreEqual("CN=Test Intermediate", certificates.Single(x => !x.HasPrivateKey).Subject);

        // asked again, the order gives what it has, and signs nothing more
        Assert.AreSame(pfx, await order.Validate());
        Assert.AreEqual(1, orderContext.FinalizeCount);
    }

    [TestMethod]
    public void Pfx_has_the_key_the_certificate_and_its_issuer()
    {
        var (certificatePem, issuerPem, keyPem) = AcmeTestUtil.CreateChain(Domain);

        var pfx = AcmeOrder.ToPfx(certificatePem, [issuerPem], keyPem);

        var certificates = X509CertificateLoader.LoadPkcs12Collection(pfx, password: null);
        Assert.AreEqual(2, certificates.Count);
        var certificate = certificates.Single(x => x.HasPrivateKey);
        Assert.AreEqual($"CN={Domain}", certificate.Subject);
        Assert.AreEqual("CN=Test Intermediate", certificates.Single(x => !x.HasPrivateKey).Subject);
    }

    private class TestOrderContext(string domain) : IOrderContext
    {
        public byte[]? Request { get; private set; }
        public Uri Location { get; } = new("https://acme.test/order");
        public int RetryAfter => 0;

        public Task<Order> Resource()
        {
            return Task.FromResult(new Order
            {
                Status = OrderStatus.Ready,
                Identifiers = [new Identifier { Type = IdentifierType.Dns, Value = domain }]
            });
        }

        public Task<IEnumerable<IAuthorizationContext>> Authorizations() => throw new NotSupportedException();

        public Task<Order> Finalize(byte[] csr)
        {
            Request = csr;
            return Task.FromResult(new Order { Status = OrderStatus.Valid });
        }

        public Task<CertificateChain> Download(string? preferredChain = null) =>
            throw new NotSupportedException("The request has been sent.");
    }

    // An order an intermediate of its own signs, as an authority signs the request it is sent: the certificate has the
    // request's key, so it goes with the key Certes made for it.
    private sealed class SigningOrderContext(string domain) : IOrderContext
    {
        private readonly ECDsa _issuerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private byte[]? _request;
        public int FinalizeCount { get; private set; }
        public Uri Location { get; } = new("https://acme.test/order");
        public int RetryAfter => 0;

        public Task<Order> Resource()
        {
            return Task.FromResult(new Order
            {
                Status = _request == null ? OrderStatus.Ready : OrderStatus.Valid,
                Identifiers = [new Identifier { Type = IdentifierType.Dns, Value = domain }]
            });
        }

        public Task<IEnumerable<IAuthorizationContext>> Authorizations() => throw new NotSupportedException();

        public Task<Order> Finalize(byte[] csr)
        {
            FinalizeCount++;
            _request = csr;
            return Task.FromResult(new Order { Status = OrderStatus.Valid });
        }

        public Task<CertificateChain> Download(string? preferredChain = null)
        {
            var request = CertificateRequest.LoadSigningRequest(
                _request ?? throw new InvalidOperationException("No request has been sent."), HashAlgorithmName.SHA256);
            var issuerRequest = new CertificateRequest("CN=Test Intermediate", _issuerKey, HashAlgorithmName.SHA256);
            issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using var issuer = issuerRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
            using var certificate = request.Create(issuer, DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(90), RandomNumberGenerator.GetBytes(16));
            return Task.FromResult(new CertificateChain(certificate.ExportCertificatePem() + "\n" + issuer.ExportCertificatePem()));
        }
    }

    // the first status answers the request to check; each read after it takes the next, and the last one stays
    private class TestChallengeContext(params ChallengeStatus[] statuses) : IChallengeContext
    {
        private int _index;
        public Uri Location { get; } = new("https://acme.test/challenge");
        public int RetryAfter => 0;
        public string Type => ChallengeTypes.Http01;
        public string Token => "token";
        public string KeyAuthz => "token.key";
        public AcmeError? Error { get; init; }
        public int ValidateCount { get; private set; }
        public int ResourceCount { get; private set; }

        public Task<Challenge> Validate()
        {
            ValidateCount++;
            return Next();
        }

        public Task<Challenge> Resource()
        {
            ResourceCount++;
            return Next();
        }

        private Task<Challenge> Next()
        {
            var status = statuses[Math.Min(_index++, statuses.Length - 1)];
            return Task.FromResult(new Challenge { Status = status, Error = Error });
        }
    }
}
