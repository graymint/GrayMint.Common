using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GrayMint.Common.Test;

// The API's description takes its words from the doc comments: the app's (TestWebApp's ItemsController and its
// classes) and its packages' (ListResult and Patch, in GrayMint.Common), with the title, version and introduction the
// app gives AddGrayMintOpenApi.
[TestClass]
public class ApiDocsTest : TestBase
{
    private static JsonObject? _document;

    private static JsonObject Document =>
        _document ?? throw new InvalidOperationException("The class has not been initialized.");

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext testContext)
    {
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        _document = JsonNode.Parse(json)?.AsObject() ?? throw new AssertFailedException("No description.");
    }

    [TestMethod]
    public void Document_has_the_apps_title_version_and_introduction()
    {
        Assert.AreEqual("Test Shop", (string?)Document["info"]?["title"]);
        Assert.AreEqual("1.2.3", (string?)Document["info"]?["version"]);
        Assert.AreEqual("The shop's API, for its tests.", (string?)Document["info"]?["description"]);
    }

    [TestMethod]
    public void Section_has_its_controllers_words_name_and_place()
    {
        var tags = Document["tags"]?.AsArray() ?? [];
        var items = tags.FirstOrDefault(x => (string?)x?["name"] == "Items");
        Assert.AreEqual("The shop's items.\n\nEach item has a name and a size.", (string?)items?["description"]);
        Assert.AreEqual("Shop items", (string?)items?["x-displayName"]);

        // a section with a place comes before one without
        Assert.AreEqual("Items", (string?)tags.FirstOrDefault()?["name"]);
    }

    [TestMethod]
    public void Operation_has_its_actions_words()
    {
        var get = Operation("/api/items/{itemId}", "get");
        Assert.AreEqual("Get an item", (string?)get["summary"]);
        Assert.AreEqual("The item, with its size.", (string?)get["description"]);
        Assert.AreEqual("The item's id, such as \"42\".", (string?)get["parameters"]?[0]?["description"]);
        Assert.AreEqual("The item.", (string?)get["responses"]?["200"]?["description"]);

        var create = Operation("/api/items", "post");
        Assert.AreEqual("The item to add.", (string?)create["requestBody"]?["description"]);

        // the same JSON body under the framework's other names is left out
        var content = create["requestBody"]?["content"]?.AsObject()
                      ?? throw new AssertFailedException("The body has no content.");

        Assert.AreSequenceEqual(["application/json"], content.Select(x => x.Key));
    }

    [TestMethod]
    public void Only_an_operation_that_takes_a_token_asks_for_one()
    {
        Assert.IsNotNull(Operation("/api/items/{itemId}", "get")["security"]?[0]?["Bearer"]);
        Assert.IsTrue(Operation("/api/items/count", "get")["security"] is null or JsonArray { Count: 0 });
    }

    [TestMethod]
    public void Schema_has_its_types_words()
    {
        var item = Schema("Item");
        Assert.AreEqual("An item of the shop.", (string?)item["description"]);
        Assert.AreEqual("The item's name.", (string?)item["properties"]?["name"]?["description"]);

        // a property whose type is a schema of its own has its words beside the reference
        Assert.AreEqual("How big the item is.", (string?)item["properties"]?["size"]?["description"]);

        // an enum lists its values with theirs
        Assert.AreEqual("How big an item is.\n\n- `Small`: Fits in a hand.\n- `Large`: Takes two hands.",
            (string?)Schema("ItemSize")["description"]);
    }

    [TestMethod]
    public void Generic_type_of_a_package_has_its_words()
    {
        var listResult = Schema("ListResultOfItem");
        Assert.AreEqual("One page of a list, and how many items match in all.", (string?)listResult["description"]);
        Assert.AreEqual("The items of this page.", (string?)listResult["properties"]?["items"]?["description"]);

        var patch = Schema("PatchOfString");
        Assert.AreEqual("The field's new value.", (string?)patch["properties"]?["value"]?["description"]);
    }

    private static JsonObject Operation(string path, string method)
    {
        return Document["paths"]?[path]?[method]?.AsObject()
               ?? throw new AssertFailedException($"{method} {path} is not in the description.");
    }

    private static JsonObject Schema(string name)
    {
        return Document["components"]?["schemas"]?[name]?.AsObject()
               ?? throw new AssertFailedException($"{name} is not in the description.");
    }
}
