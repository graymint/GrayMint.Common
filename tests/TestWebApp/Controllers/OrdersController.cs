using Microsoft.AspNetCore.Mvc;
using TestWebApp.Dtos.Orders;

namespace TestWebApp.Controllers;

// the shapes a client generator needs as NSwag's own document had them (ClientDocumentTest); no doc comments, so its
// responses have no words of their own
[ApiController]
public class OrdersController : ControllerBase
{
    [HttpGet("api/shop")]
    public Shop GetShop()
    {
        return new Shop();
    }

    [HttpGet("api/orders")]
    public Task<Order[]?> List()
    {
        return Task.FromResult<Order[]?>(null);
    }

    [HttpGet("api/orders/latest")]
    public Task<Order?> GetLatest()
    {
        return Task.FromResult<Order?>(null);
    }

    [HttpGet("api/orders/total")]
    public Task<decimal?> GetTotal()
    {
        return Task.FromResult<decimal?>(null);
    }

    [HttpGet("api/orders/count")]
    [ProducesResponseType<int?>(200)]
    public IActionResult Count()
    {
        return Ok(null);
    }

    [HttpGet("api/orders/summary")]
    public OrderSummary GetSummary()
    {
        return new OrderSummary { OrderId = "1" };
    }

    [HttpGet("api/orders/legacy")]
    public LegacyBox GetLegacy()
    {
        return new LegacyBox();
    }

    [HttpGet("api/orders/callback")]
    [HttpPost("api/orders/callback")]
    public ContentResult Callback()
    {
        return Content("done");
    }

    // orderId comes from the route alone, so it is described after the action's own parameters
    [HttpPost("api/shops/{shopId}/orders/{orderId}/notes")]
    public void AddNote(string shopId, Order? order = null, string? author = null)
    {
    }

    [HttpPost("api/orders/upload")]
    public void Upload([FromForm] OrderUploadRequest request)
    {
    }
}
