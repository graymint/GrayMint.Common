using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

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

    // a struct's first use, as a nullable value
    public OrderTotals? Totals { get; init; }

    // any JSON
    public JsonElement? Data { get; init; }

    // never read as null, though its setter takes null
    [AllowNull]
    public Dictionary<string, string?> Tags {
        get;
        init => field = value ?? [];
    } = [];

    public OrderPriority Priority { get; init; }
    public OrderChannel Channel { get; init; }
}
