using GrayMint.Common.Utils;

namespace TestWebApp.Dtos;

/// <summary>
/// An item to add.
/// </summary>
public class ItemCreateRequest
{
    /// <summary>The item's name.</summary>
    public required string Name { get; init; }

    /// <summary>How big the item is.</summary>
    public required ItemSize Size { get; init; }

    /// <summary>A note on the item, if it has one.</summary>
    public Patch<string>? Note { get; init; }
}
