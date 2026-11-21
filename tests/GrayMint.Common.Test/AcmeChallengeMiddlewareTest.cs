using System.Net;
using GrayMint.Common.Acme;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace GrayMint.Common.Test;

[TestClass]
public class AcmeChallengeMiddlewareTest
{
    private const string Domain = "checkout.example.com";
    private const string Token = "abc_DEF-123";
    private const string KeyAuthorization = "abc_DEF-123.thumbprint";

    private static async Task<WebApplication> StartApp(List<string> asked)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseAcmeChallenge((domain, token, _) =>
        {
            asked.Add($"{domain} {token}");
            return ValueTask.FromResult(domain == Domain && token == Token ? KeyAuthorization : null);
        });
        app.Run(context => context.Response.WriteAsync("the app"));
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode, string)> Get(WebApplication app, string host, string path,
        HttpMethod? method = null)
    {
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        request.Headers.Host = host;
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Challenge_is_answered_on_its_domain()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        var (status, body) = await Get(app, "CHECKOUT.example.com.", AcmeSelfCheck.PathPrefix + Token);

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(KeyAuthorization, body);
        CollectionAssert.AreEqual(new[] { $"{Domain} {Token}" }, asked);
    }

    [TestMethod]
    public async Task Unknown_challenge_is_not_found_and_goes_no_further()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        Assert.AreEqual((HttpStatusCode.NotFound, ""), await Get(app, "other.example.com", AcmeSelfCheck.PathPrefix + Token));
        Assert.AreEqual((HttpStatusCode.NotFound, ""), await Get(app, Domain, AcmeSelfCheck.PathPrefix + "other"));
        Assert.AreEqual(2, asked.Count);
    }

    [TestMethod]
    public async Task What_is_no_token_is_not_asked_about()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        Assert.AreEqual(HttpStatusCode.NotFound, (await Get(app, Domain, AcmeSelfCheck.PathPrefix)).Item1);
        Assert.AreEqual(HttpStatusCode.NotFound, (await Get(app, Domain, AcmeSelfCheck.PathPrefix + "a.b")).Item1);
        Assert.AreEqual(HttpStatusCode.NotFound, (await Get(app, Domain, AcmeSelfCheck.PathPrefix + Token + "/x")).Item1);
        Assert.AreEqual(HttpStatusCode.NotFound,
            (await Get(app, Domain, AcmeSelfCheck.PathPrefix + new string('a', 300))).Item1);
        Assert.AreEqual(HttpStatusCode.NotFound,
            (await Get(app, Domain, AcmeSelfCheck.PathPrefix + Token, HttpMethod.Post)).Item1);
        Assert.AreEqual(0, asked.Count);
    }

    [TestMethod]
    public async Task Other_requests_go_on()
    {
        var asked = new List<string>();
        await using var app = await StartApp(asked);

        Assert.AreEqual((HttpStatusCode.OK, "the app"), await Get(app, Domain, "/"));
        Assert.AreEqual((HttpStatusCode.OK, "the app"), await Get(app, Domain, "/.well-known/other"));
        Assert.AreEqual(0, asked.Count);
    }
}
