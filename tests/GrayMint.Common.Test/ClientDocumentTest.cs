using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.Test;

// The API's description types and names what a client generator reads as NSwag's own document did, where the
// framework's differs (TestWebApp's OrdersController).
[TestClass]
public class ClientDocumentTest
{
    private static JsonObject _document = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext testContext)
    {
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        _document = JsonNode.Parse(json)?.AsObject() ?? throw new AssertFailedException("No description.");
    }

    [TestMethod]
    public void Enum_never_null_by_its_nullable_use()
    {
        // the enum's own schema: one type, and its values only
        var state = Schema("OrderState");
        Assert.IsInstanceOfType<JsonValue>(state["type"]);
        Assert.AreEqual(2, state["enum"]!.AsArray().Count);
        Assert.IsFalse(state["enum"]!.AsArray().Any(x => x is null));

        var order = Schema("Order")["properties"]!;
        Assert.AreEqual("#/components/schemas/OrderState", (string?)order["state"]?["$ref"]);
        Assert.AreEqual("null", (string?)order["previousState"]?["oneOf"]?[0]?["type"]);
    }

    [TestMethod]
    public void Decimal_is_a_decimal()
    {
        Assert.AreEqual("decimal", (string?)Schema("Order")["properties"]?["price"]?["format"]);
    }

    [TestMethod]
    public void Dictionary_of_any_value_has_values()
    {
        Assert.IsNotNull(Schema("Order")["properties"]?["extras"]?["additionalProperties"]);
    }

    [TestMethod]
    public void Any_value_that_may_be_null_says_so()
    {
        Assert.IsTrue((bool?)Schema("Order")["properties"]?["note"]?["nullable"]);
    }

    [TestMethod]
    public void Schema_the_framework_empties_is_built_again()
    {
        // the framework leaves Order's lines as {}, with no schema for OrderLine
        var lines = Schema("Order")["properties"]?["lines"];
        Assert.AreEqual("#/components/schemas/OrderLine", (string?)lines?["items"]?["$ref"]);

        var line = Schema("OrderLine")["properties"]!;
        Assert.AreEqual("integer", (string?)line["quantity"]?["type"]);
        Assert.AreEqual("#/components/schemas/Order", (string?)line["order"]?["oneOf"]?[1]?["$ref"]);
    }

    [TestMethod]
    public void Class_written_as_a_value_stays_a_value_where_schemas_are_built_again()
    {
        var clientIp = Schema("Order")["properties"]?["clientIp"];
        var schema = (string?)clientIp?["$ref"] is { } reference ? Schema(reference.Split('/')[^1]) : clientIp;
        Assert.AreEqual("string", (string?)schema?["type"]);
        Assert.IsNull(schema?["properties"]);
    }

    [TestMethod]
    public void Two_types_of_one_name_have_a_schema_each()
    {
        Assert.IsNotNull(Schema("Label")["properties"]?["text"]);
        Assert.IsNotNull(Schema("Label2")["properties"]?["message"]);
    }

    [TestMethod]
    public void Class_keeps_its_base()
    {
        var parts = Schema("OrderSummary")["allOf"]!.AsArray();
        Assert.AreEqual("#/components/schemas/OrderBase", (string?)parts[0]?["$ref"]);
        Assert.IsNotNull(parts[1]?["properties"]?["lineCount"]);
        Assert.IsNull(parts[1]?["properties"]?["orderId"]);
        Assert.IsNotNull(Schema("OrderBase")["properties"]?["orderId"]);
    }

    [TestMethod]
    public void Reference_without_nullable_annotations_is_nullable()
    {
        var label = Schema("LegacyBox")["properties"]?["label"];
        Assert.AreEqual("null", (string?)label?["oneOf"]?[0]?["type"]);
        Assert.AreEqual("#/components/schemas/Label", (string?)label?["oneOf"]?[1]?["$ref"]);
    }

    [TestMethod]
    public void Nullable_collection_has_nullable_items()
    {
        var items = ResponseSchema("/api/orders", "get")["items"];
        Assert.AreEqual("null", (string?)items?["oneOf"]?[0]?["type"]);
        Assert.AreEqual("#/components/schemas/Order", (string?)items?["oneOf"]?[1]?["$ref"]);
    }

    [TestMethod]
    public void Object_or_collection_answered_is_never_null()
    {
        // as NSwag's document had them, though the actions declare them nullable (Task<Order?>, Task<Order[]?>)
        var latest = ResponseSchema("/api/orders/latest", "get");
        Assert.IsNull(latest["oneOf"]);
        Assert.AreEqual("#/components/schemas/Order", (string?)latest["$ref"]);

        var orders = ResponseSchema("/api/orders", "get")["type"];
        Assert.AreEqual("array", orders is JsonValue ? (string?)orders : orders?.ToJsonString());
    }

    [TestMethod]
    public void Nullable_value_an_attribute_declares_stays_nullable()
    {
        // the action answers an IActionResult; ProducesResponseType<int?> says what
        var count = ResponseSchema("/api/orders/count", "get");
        CollectionAssert.AreEquivalent(new[] { "null", "integer" },
            count["type"]!.AsArray().Select(x => (string?)x).ToArray());
    }

    [TestMethod]
    public void Nullable_value_answered_is_the_values_schema()
    {
        var total = ResponseSchema("/api/orders/total", "get");
        Assert.IsNull(total["oneOf"]);
        Assert.AreEqual("decimal", (string?)total["format"]);
        CollectionAssert.AreEquivalent(new[] { "null", "number" },
            total["type"]!.AsArray().Select(x => (string?)x).ToArray());
    }

    [TestMethod]
    public void Action_of_several_methods_has_an_id_per_method_and_answers_a_file()
    {
        var get = Operation("/api/orders/callback", "get");
        Assert.AreEqual("Orders_CallbackGET", (string?)get["operationId"]);
        Assert.AreEqual("Orders_CallbackPOST", (string?)Operation("/api/orders/callback", "post")["operationId"]);
        Assert.IsNotNull(get["responses"]?["200"]?["content"]?["application/octet-stream"]);
    }

    [TestMethod]
    public void Parameters_and_body_have_their_places()
    {
        var operation = Operation("/api/shops/{shopId}/orders/{orderId}/notes", "post");
        var positions = operation["parameters"]!.AsArray()
            .ToDictionary(x => (string)x!["name"]!, x => (int?)x!["x-position"]);
        Assert.AreEqual(1, positions["shopId"]);
        Assert.AreEqual(3, positions["author"]);
        Assert.AreEqual(4, positions["orderId"]);

        // a body that may be left out is not required, and not nullable
        var body = operation["requestBody"]!;
        Assert.AreEqual(2, (int?)body["x-position"]);
        Assert.AreNotEqual(true, (bool?)body["required"]);
        Assert.AreEqual("#/components/schemas/Order", (string?)body["content"]?["application/json"]?["schema"]?["$ref"]);
    }

    [TestMethod]
    public void Form_file_is_an_optional_file()
    {
        var form = Operation("/api/orders/upload", "post")["requestBody"]?["content"]?["multipart/form-data"]?["schema"];
        var file = form?["properties"]?["File"];
        Assert.AreEqual("binary", (string?)file?["format"]);
        CollectionAssert.Contains(file?["type"]!.AsArray().Select(x => (string?)x).ToArray(), "null");
        Assert.IsFalse(form?["required"]?.AsArray().Any(x => (string?)x == "File") == true);
        Assert.IsNull(_document["components"]?["schemas"]?["IFormFile"]);
    }

    [TestMethod]
    public void Response_without_words_has_none()
    {
        Assert.AreEqual("", (string?)Operation("/api/orders/total", "get")["responses"]?["200"]?["description"]);
    }

    [TestMethod]
    public async Task Json_body_is_described_once_not_once_per_alias()
    {
        // the framework makes a body's schema for each media type the API's description gives it, so the description
        // the document is made from gives JSON alone, as the document does
        await using var factory = new WebApplicationFactory<TestWebApp.Program>();
        var create = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items
            .SelectMany(x => x.Items)
            .Single(x => x.HttpMethod == "POST" && x.RelativePath == "api/items");

        CollectionAssert.AreEqual(new[] { "application/json" },
            create.SupportedRequestFormats.Select(x => x.MediaType).ToArray());
        CollectionAssert.AreEqual(new[] { "application/json" },
            create.SupportedResponseTypes.Single(x => x.StatusCode == 200).ApiResponseFormats.Select(x => x.MediaType).ToArray());
    }

    private static JsonObject ResponseSchema(string path, string method)
    {
        return Operation(path, method)["responses"]?["200"]?["content"]?["application/json"]?["schema"]?.AsObject()
               ?? throw new AssertFailedException($"{method} {path} answers no JSON.");
    }

    private static JsonObject Operation(string path, string method)
    {
        return _document["paths"]?[path]?[method]?.AsObject()
               ?? throw new AssertFailedException($"{method} {path} is not in the description.");
    }

    private static JsonObject Schema(string name)
    {
        return _document["components"]?["schemas"]?[name]?.AsObject()
               ?? throw new AssertFailedException($"{name} is not in the description.");
    }
}
