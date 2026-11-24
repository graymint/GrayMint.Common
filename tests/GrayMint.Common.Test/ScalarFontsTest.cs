using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GrayMint.Common.Test;

// Scalar's page shows its own fonts, Inter and JetBrains Mono, and the service serves them, so a reader's browser calls
// nothing but the service.
[TestClass]
public partial class ScalarFontsTest : TestBase
{
    private static readonly Uri PageUri = new("http://localhost/scalar/");

    [TestMethod]
    public async Task Page_takes_its_fonts_from_the_service()
    {
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        using var client = factory.CreateClient();

        var page = await client.GetStringAsync(PageUri.AbsolutePath);
        var configuration = ReadConfiguration(page);
        Assert.IsFalse(configuration.GetProperty("withDefaultFonts").GetBoolean(), "Scalar's CDN fonts are off.");
        var css = configuration.GetProperty("customCss").GetString() ?? "";

        var families = FontFamilyRegex().Matches(css).Select(x => x.Groups[1].Value).Distinct();
        Assert.AreSequenceEqual(["Inter", "JetBrains Mono"], families, SequenceOrder.InAnyOrder);

        var urls = FontUrlRegex().Matches(css).Select(x => x.Groups[1].Value).ToArray();
        Assert.HasCount(13, urls);
        foreach (var url in urls)
        {
            // relative to the page, so the browser asks the service that served it
            Assert.IsFalse(url.Contains(':') || url.StartsWith('/'), url);

            using var response = await client.GetAsync(new Uri(PageUri, url).AbsolutePath);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, url);
            Assert.AreEqual("font/woff2", response.Content.Headers.ContentType?.MediaType, url);
            var content = await response.Content.ReadAsByteArrayAsync();
            Assert.AreEqual("wOF2", Encoding.ASCII.GetString(content, 0, 4), url);
        }
    }

    [TestMethod]
    public async Task Font_is_sent_again_only_when_it_changed()
    {
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        using var client = factory.CreateClient();
        const string fontPath = "/scalar/fonts/inter-latin-wght-normal.woff2";

        using var first = await client.GetAsync(fontPath);
        var entityTag = first.Headers.ETag ?? throw new AssertFailedException("The font has no ETag.");

        using var request = new HttpRequestMessage(HttpMethod.Get, fontPath);
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(entityTag.Tag));
        using var second = await client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.NotModified, second.StatusCode);
    }

    [TestMethod]
    public async Task Only_the_fonts_are_served_and_the_api_description_names_none()
    {
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/scalar/fonts/GrayMint.Common.OpenApi.dll");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var description = await client.GetStringAsync("/openapi/v1.json");
        Assert.DoesNotContain("/scalar/fonts", description);
    }

    // Scalar's page hands its configuration to its script as JSON, the third argument of initialize()
    private static JsonElement ReadConfiguration(string page)
    {
        var start = page.IndexOf('{', page.IndexOf("initialize(", StringComparison.Ordinal));
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(page[start..]));
        return JsonElement.ParseValue(ref reader);
    }

    [GeneratedRegex("font-family: \"([^\"]+)\"")]
    private static partial Regex FontFamilyRegex();

    [GeneratedRegex("url\\(\"([^\"]+)\"\\)")]
    private static partial Regex FontUrlRegex();
}
