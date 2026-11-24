using System.Net;
using GrayMint.Common.Acme;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace GrayMint.Common.Test;

[TestClass]
public class AcmeChallengeMiddlewareTest : TestBase
{
    private const string Domain = "checkout.example.com";
    private const string Token = "abc_DEF-123";
    private const string KeyAuthorization = "abc_DEF-123.thumbprint";

    private async Task<WebApplication> StartApp(List<string> asked)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseAcmeChallenge((domain, token, _) =>
        {
            asked.Add($"{domain} {token}");
            return ValueTask.FromResult(domain == Domain && token == Token ? KeyAuthorization : null);
        });

        app.Run(context => context.Response.WriteAsync("the app", context.RequestAborted));
        await app.StartAsync();
        return app;
    }

    private async Task<TestResponse> Get(WebApplication app, string host, string path, HttpMethod? method = null)
    {
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        request.Headers.Host = host;
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        return new TestResponse(response.StatusCode, body);
    }

    private async Task<HttpStatusCode> GetStatus(WebApplication app, string host, string path,
        HttpMethod? method = null)
    {
        var response = await Get(app, host, path, method);
        return response.StatusCode;
    }

    [TestMethod]
    public async Task Challenge_is_answered_on_its_domain()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        var (status, body) = await Get(app, "CHECKOUT.example.com.", AcmeSelfCheck.PathPrefix + Token);

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(KeyAuthorization, body);
        Assert.AreSequenceEqual([$"{Domain} {Token}"], asked);
    }

    [TestMethod]
    public async Task Unknown_challenge_is_not_found_and_goes_no_further()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        var notFound = new TestResponse(HttpStatusCode.NotFound, "");
        Assert.AreEqual(notFound, await Get(app, "other.example.com", AcmeSelfCheck.PathPrefix + Token));
        Assert.AreEqual(notFound, await Get(app, Domain, AcmeSelfCheck.PathPrefix + "other"));
        Assert.HasCount(2, asked);
    }

    [TestMethod]
    public async Task What_is_no_token_is_not_asked_about()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        Assert.AreEqual(HttpStatusCode.NotFound, await GetStatus(app, Domain, AcmeSelfCheck.PathPrefix));
        Assert.AreEqual(HttpStatusCode.NotFound, await GetStatus(app, Domain, AcmeSelfCheck.PathPrefix + "a.b"));
        Assert.AreEqual(HttpStatusCode.NotFound, await GetStatus(app, Domain, AcmeSelfCheck.PathPrefix + Token + "/x"));
        Assert.AreEqual(HttpStatusCode.NotFound,
            await GetStatus(app, Domain, AcmeSelfCheck.PathPrefix + new string('a', 300)));

        Assert.AreEqual(HttpStatusCode.NotFound,
            await GetStatus(app, Domain, AcmeSelfCheck.PathPrefix + Token, HttpMethod.Post));

        Assert.IsEmpty(asked);
    }

    [TestMethod]
    public async Task Other_requests_go_on()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        Assert.AreEqual(new TestResponse(HttpStatusCode.OK, "the app"), await Get(app, Domain, "/"));
        Assert.AreEqual(new TestResponse(HttpStatusCode.OK, "the app"), await Get(app, Domain, "/.well-known/other"));
        Assert.IsEmpty(asked);
    }

    private sealed record TestResponse(HttpStatusCode StatusCode, string Body);
}
