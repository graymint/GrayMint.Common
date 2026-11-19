namespace GrayMint.Common.OpenApi;

public class AddOpenApiOptions
{
    /// <summary>The API's name: the document's title and the Scalar page's.</summary>
    public string? Title { get; init; }

    /// <summary>The document's introduction, in Markdown, which the reference shows above the sections.</summary>
    public string? Description { get; init; }

    /// <summary>The API's version, which the document names; 1.0.0 without it.</summary>
    public string? Version { get; init; }

    public bool AddScalar { get; init; } = true;
}
