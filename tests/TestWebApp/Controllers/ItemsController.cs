using GrayMint.Common.Generics;
using GrayMint.Common.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestWebApp.Dtos;

namespace TestWebApp.Controllers;

/// <summary>
/// The shop's items.
/// </summary>
/// <remarks>
/// Each item has a name and a size.
/// </remarks>
[ApiController]
[Route("api/items")]
[ApiSection("Shop items", 10)]
public class ItemsController : ControllerBase
{
    /// <summary>
    /// Get an item
    /// </summary>
    /// <remarks>
    /// The item, with its size.
    /// </remarks>
    /// <param name="itemId">The item's id, such as "42".</param>
    /// <returns>The item.</returns>
    [HttpGet("{itemId}")]
    public Item Get(string itemId)
    {
        return new Item { ItemId = itemId, Name = "cup", Size = ItemSize.Small };
    }

    /// <summary>
    /// Add an item
    /// </summary>
    /// <param name="request">The item to add.</param>
    /// <returns>The new item.</returns>
    [HttpPost]
    public Item Create(ItemCreateRequest request)
    {
        return new Item { ItemId = "1", Name = request.Name, Size = request.Size };
    }

    /// <summary>
    /// List the items
    /// </summary>
    /// <param name="count">How many items a page holds.</param>
    [HttpGet]
    public ListResult<Item> List(int count = 10)
    {
        return new ListResult<Item> { Items = [], TotalCount = 0 };
    }

    /// <summary>
    /// Count the items
    /// </summary>
    /// <remarks>
    /// Open to anyone.
    /// </remarks>
    [HttpGet("count")]
    [AllowAnonymous]
    public int Count()
    {
        return 0;
    }
}
