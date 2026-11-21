namespace TestWebApp.Dtos.Orders;

public class OrderLine
{
    public int Quantity { get; init; }
    public Order? Order { get; init; }
}
