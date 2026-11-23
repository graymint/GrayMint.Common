using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using TestWebApp.Client;

namespace GrayMint.Common.Test;

// TestWebApp.Client is made on its build by GrayMint.Common.OpenApi.ClientGenerator's targets, from the document
// TestWebApp writes: the C# client calls the API, and the TypeScript client is written beside it.
[TestClass]
public class ApiClientGeneratorTest
{
    private static WebApplicationFactory<TestWebApp.Program> _factory = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        _factory = new WebApplicationFactory<TestWebApp.Program>();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Client_calls_the_api()
    {
        var itemsClient = new ItemsClient(_factory.CreateClient());

        var item = await itemsClient.CreateAsync(new ItemCreateRequest { Name = "cup", Size = ItemSize.Large });
        Assert.AreEqual("cup", item.Name);
        Assert.AreEqual(ItemSize.Large, item.Size);

        item = await itemsClient.GetAsync("42");
        Assert.AreEqual("42", item.ItemId);
    }

    [TestMethod]
    public async Task Client_takes_the_base_class_defaults()
    {
        // the base class's defaults reach the request only through its async hooks, which the settings ask for
        var handler = new RecordingHandler(_factory.Server.CreateHandler());
        var itemsClient = new ItemsClient(new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress }) {
            DefaultAuthorization = new AuthenticationHeaderValue("Bearer", "test-token")
        };

        await itemsClient.CountAsync();
        Assert.AreEqual("Bearer test-token", handler.LastRequest?.Headers.Authorization?.ToString());
    }

    [TestMethod]
    public void Clients_are_written_beside_the_document()
    {
        var apiDir = Path.Combine(FindRepoRoot(), "tests", "TestWebApp.Client", "Api");
        Assert.IsTrue(File.Exists(Path.Combine(apiDir, "TestWebApp.json")));
        Assert.IsTrue(File.Exists(Path.Combine(apiDir, "Api.cs")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(apiDir, "Api.ts")), "export class ItemsClient");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GrayMint.Common.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new AssertFailedException("The repo's root was not found.");
    }

    private class RecordingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
