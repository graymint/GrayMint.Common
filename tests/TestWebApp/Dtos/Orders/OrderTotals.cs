namespace TestWebApp.Dtos.Orders;

// a struct, which Order meets first as a nullable value
public readonly record struct OrderTotals
{
    public long Items { get; init; }
    public long Paid { get; init; }
}
