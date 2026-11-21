using System.Net;

namespace TestWebApp.Dtos.Orders;

public class Order
{
    public required string OrderId { get; init; }

    // the nullable use of the enum comes first, before the one that is never null
    public OrderState? PreviousState { get; init; }
    public OrderState State { get; init; }
    public decimal Price { get; init; }
    public Dictionary<string, object> Extras { get; init; } = [];
    public object? Note { get; init; }
    public Label? Label { get; init; }

    // a class written as a string
    public required IPAddress ClientIp { get; init; }
    public ICollection<OrderLine>? Lines { get; init; }
}
