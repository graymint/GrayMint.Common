namespace TestWebApp.Dtos.Orders;

// meets Order first in a collection, whose lines refer back to their order
public class Shop
{
    public ICollection<Order>? Orders { get; init; }
}
