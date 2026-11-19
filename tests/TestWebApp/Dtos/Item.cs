namespace TestWebApp.Dtos;

/// <summary>
/// An item of the shop.
/// </summary>
public class Item
{
    /// <summary>The item's id.</summary>
    public required string ItemId { get; init; }

    /// <summary>The item's name.</summary>
    public required string Name { get; init; }

    /// <summary>How big the item is.</summary>
    public required ItemSize Size { get; init; }
}
