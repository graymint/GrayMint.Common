using Microsoft.AspNetCore.Mvc;
using TestWebApp.Dtos.Orders;

namespace TestWebApp.Controllers;

// the shapes a client generator needs as NSwag's own document had them (ClientDocumentTest); no doc comments, so its
// responses have no words of their own
[ApiController]
[Route("api")]
public class OrdersController : ControllerBase
{
    [HttpGet("shop")]
    public Shop GetShop()
    {
        return new Shop();
    }

    [HttpGet("orders")]
    public Task<Order[]?> List()
    {
        return Task.FromResult<Order[]?>(null);
    }

    [HttpGet("orders/latest")]
    public Task<Order?> GetLatest()
    {
        return Task.FromResult<Order?>(null);
    }

    [HttpGet("orders/total")]
    public Task<decimal?> GetTotal()
    {
        return Task.FromResult<decimal?>(null);
    }

    [HttpGet("orders/count")]
    [ProducesResponseType<int?>(200)]
    public IActionResult Count()
    {
        return Ok(null);
    }

    [HttpGet("orders/summary")]
    public OrderSummary GetSummary()
    {
        return new OrderSummary { OrderId = "1" };
    }

    [HttpGet("orders/legacy")]
    public LegacyBox GetLegacy()
    {
        return new LegacyBox();
    }

    [HttpGet("orders/callback")]
    [HttpPost("orders/callback")]
    public ContentResult Callback()
    {
        return Content("done");
    }

    // orderId comes from the route alone, so it is described after the action's own parameters
#pragma warning disable ASP0018
    // ReSharper disable once RouteTemplates.RouteParameterIsNotPassedToMethod
    [HttpPost("shops/{shopId}/orders/{orderId}/notes")]
#pragma warning restore ASP0018
    public void AddNote(string shopId, Order? order = null, string? author = null)
    {
    }

    [HttpPost("orders/upload")]
    public void Upload([FromForm] OrderUploadRequest request)
    {
    }
}
