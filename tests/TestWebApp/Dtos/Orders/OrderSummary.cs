namespace TestWebApp.Dtos.Orders;

public class OrderSummary : OrderBase
{
    public int LineCount { get; init; }
    public Gift.Label? GiftLabel { get; init; }
}
