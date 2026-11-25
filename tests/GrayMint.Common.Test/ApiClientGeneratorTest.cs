using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TestWebApp.Client;

namespace GrayMint.Common.Test;

// TestWebApp.Client is made on its build by GrayMint.Common.OpenApi.ClientGenerator's targets, from the document
// TestWebApp writes: the C# client calls the API, and the TypeScript client is written beside it.
[TestClass]
public class ApiClientGeneratorTest : TestBase
{
    private static readonly WebApplicationFactory<TestWebApp.Program> Factory = new();

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await Factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Client_calls_the_api()
    {
        var itemsClient = new ItemsClient(Factory.CreateClient());

        var item = await itemsClient.CreateAsync(new ItemCreateRequest { Name = "cup", Size = ItemSize.Large });
        Assert.AreEqual("cup", item.Name);
        Assert.AreEqual(ItemSize.Large, item.Size);

        item = await itemsClient.GetAsync("42");
        Assert.AreEqual("42", item.ItemId);
    }

    [TestMethod]
    public async Task Client_reads_a_list_of_enums_sent_by_name()
    {
        // NSwag gives a single enum property its by-name converter but not a list of them; the clients' options carry one
        var itemsClient = new ItemsClient(Factory.CreateClient());

        var sizes = await itemsClient.ListSizesAsync();
        CollectionAssert.AreEqual(new[] { ItemSize.Small, ItemSize.Large }, sizes.ToArray());
    }

    [TestMethod]
    public async Task Client_takes_the_base_class_defaults()
    {
        // the base class's defaults reach the request only through its async hooks, which the settings ask for
        var handler = new RecordingHandler(Factory.Server.CreateHandler());
        var itemsClient = new ItemsClient(new HttpClient(handler) { BaseAddress = Factory.Server.BaseAddress }) {
            DefaultAuthorization = new AuthenticationHeaderValue("Bearer", "test-token")
        };

        await itemsClient.CountAsync();
        Assert.AreEqual("Bearer test-token", handler.LastRequest?.Headers.Authorization?.ToString());
    }

    [TestMethod]
    public async Task Clients_are_written_beside_the_project_file()
    {
        var projectDir = Path.Combine(FindRepoRoot(), "tests", "TestWebApp.Client");
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "TestWebApp.json")));
        Assert.IsTrue(File.Exists(Path.Combine(projectDir, "Clients.cs")));
        var typeScript = await File.ReadAllTextAsync(Path.Combine(projectDir, "Clients.ts"));
        Assert.Contains("export class ItemsClient", typeScript);
    }

    // a client goes beside its project file, as Clients.*: Api/, the default before, is kept for none made there
    [TestMethod]
    [DataRow(null)]
    [DataRow("Api.cs")]
    [DataRow("Api.ts")]
    public async Task A_client_goes_beside_the_project_file(string? clientFileInApi)
    {
        var projectDir = Directory.CreateTempSubdirectory("ApiClientDirectory").FullName;
        try {
            var build = Path.Combine(FindRepoRoot(), "src", "GrayMint.Common.OpenApi.ClientGenerator", "build");
            await File.WriteAllTextAsync(Path.Combine(projectDir, "Client.csproj"), $"""
                <Project>
                  <PropertyGroup><ApiServerProject>../Server/Server.csproj</ApiServerProject></PropertyGroup>
                  <Import Project="{Path.Combine(build, "GrayMint.Common.OpenApi.ClientGenerator.targets")}" />
                </Project>
                """, TestCt);
            if (clientFileInApi != null) {
                Directory.CreateDirectory(Path.Combine(projectDir, "Api"));
                await File.WriteAllTextAsync(Path.Combine(projectDir, "Api", clientFileInApi), "", TestCt);
            }

            using var msbuild = Process.Start(new ProcessStartInfo("dotnet",
                ["msbuild", "-nologo", Path.Combine(projectDir, "Client.csproj"), "-getProperty:ApiClientDirectory",
                    "-getProperty:ApiClientFileName"]) {
                RedirectStandardOutput = true,
                Environment = { ["DOTNET_NOLOGO"] = "1" }
            }) ?? throw new AssertFailedException("dotnet did not start.");
            var output = await msbuild.StandardOutput.ReadToEndAsync(TestCt);
            await msbuild.WaitForExitAsync(TestCt);

            Assert.AreEqual(0, msbuild.ExitCode, output);
            var properties = JsonDocument.Parse(output).RootElement.GetProperty("Properties");
            Assert.AreEqual(Path.GetFullPath(projectDir).TrimEnd('\\', '/'),
                Path.GetFullPath(properties.GetProperty("ApiClientDirectory").GetString() ?? "").TrimEnd('\\', '/'));
            Assert.AreEqual("Clients", properties.GetProperty("ApiClientFileName").GetString());
        }
        finally {
            Directory.Delete(projectDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task TypeScript_comments_end_where_they_should()
    {
        // a */ in the API's words, as Item.ItemId's `*/*`, would end its /** */ comment early
        var apiFile = Path.Combine(FindRepoRoot(), "tests", "TestWebApp.Client", "Clients.ts");
        var typeScript = await File.ReadAllTextAsync(apiFile);
        Assert.Contains(@"`*\/*` names any item", typeScript);
        Assert.DoesNotContain("`*/*`", typeScript);
    }

    [TestMethod]
    public async Task Client_carries_the_api_words_as_doc_comments()
    {
        // the client's doc file, copied beside this test's assembly, has the API's words for an IDE to show
        var docFile = Path.Combine(AppContext.BaseDirectory, "TestWebApp.Client.xml");
        Assert.IsTrue(File.Exists(docFile));
        var docs = await File.ReadAllTextAsync(docFile);
        Assert.Contains("Get an item", docs);
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
