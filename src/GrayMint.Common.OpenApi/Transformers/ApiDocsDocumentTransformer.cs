using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

// The document as a whole: its title, version and introduction from the app's options, and each section's (a
// controller's) description from the controller's doc comment, with the name and place its ApiSection gives it.
internal sealed class ApiDocsDocumentTransformer(AddOpenApiOptions options) : IOpenApiDocumentTransformer
{
    // a tag's name for readers, which Scalar shows in place of the tag's own (ApiSectionAttribute)
    private const string DisplayNameKey = "x-displayName";

    // the same JSON body under other names the framework accepts and answers, which only lengthen the reference
    private static readonly string[] JsonAliases = ["text/json", "application/*+json", "text/plain"];

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        if (options.Title != null)
            document.Info.Title = options.Title;
        if (options.Version != null)
            document.Info.Version = options.Version;
        if (options.Description != null)
            document.Info.Description = options.Description;

        DescribeSections(document, context);

        var operations = document.Paths.Values
            .SelectMany(x => x.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>());
        foreach (var operation in operations)
        {
            if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody)
                RemoveJsonAliases(requestBody.Content);

            foreach (var response in operation.Responses?.Values.OfType<OpenApiResponse>() ?? [])
                if (response.Content != null)
                    RemoveJsonAliases(response.Content);
        }

        LiftWrappedDescriptions(document);
        return Task.CompletedTask;
    }

    private static void DescribeSections(OpenApiDocument document, OpenApiDocumentTransformerContext context)
    {
        var controllers = context.DescriptionGroups
            .SelectMany(x => x.Items)
            .Select(x => x.ActionDescriptor)
            .OfType<ControllerActionDescriptor>()
            .GroupBy(x => x.ControllerName)
            .ToDictionary(x => x.Key, x => x.First().ControllerTypeInfo);

        document.Tags ??= new HashSet<OpenApiTag>();
        var sections = new Dictionary<string, ApiSectionAttribute>();
        foreach (var (name, type) in controllers)
        {
            var tag = document.Tags.FirstOrDefault(x => x.Name == name);
            if (tag == null)
            {
                tag = new OpenApiTag { Name = name };
                document.Tags.Add(tag);
            }

            tag.Description ??= XmlDocs.Join(XmlDocs.Summary(type), XmlDocs.Remarks(type));

            if (type.GetCustomAttribute<ApiSectionAttribute>() is not { } section)
                continue;

            sections[name] = section;
            tag.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            tag.Extensions[DisplayNameKey] = new JsonNodeExtension(JsonValue.Create(section.Name));
        }

        // the sections in their order, then any without one by name
        document.Tags = new HashSet<OpenApiTag>(document.Tags
            .OrderBy(x => sections.TryGetValue(x.Name ?? "", out var section) ? section.Order : int.MaxValue)
            .ThenBy(x => x.Name, StringComparer.Ordinal));
    }

    // A field that may be null and whose type is another schema comes out as oneOf [null, its $ref], with the field's
    // words on the $ref inside it: they go on the field itself, where a reader's tool looks for a field's words.
    private static void LiftWrappedDescriptions(OpenApiDocument document)
    {
        var schemas = document.Components?.Schemas?.Values.OfType<OpenApiSchema>() ?? [];
        foreach (var property in schemas.SelectMany(x => x.Properties?.Values.OfType<OpenApiSchema>() ?? []))
        {
            // the field's own words, which the reference holds; its Description falls back to the type's
            if (property.Description != null || property.OneOf?.OfType<OpenApiSchemaReference>()
                    .FirstOrDefault(x => x.Reference.Description != null) is not { } wrapped)
                continue;

            property.Description = wrapped.Reference.Description;
            wrapped.Description = null;
        }
    }

    // generic, as Microsoft.OpenApi 2 (.NET 10) holds a media type as a class and 3 (.NET 11) as IOpenApiMediaType
    private static void RemoveJsonAliases<TMediaType>(IDictionary<string, TMediaType> content)
    {
        if (!content.ContainsKey("application/json"))
            return;

        foreach (var alias in JsonAliases)
            content.Remove(alias);
    }
}
