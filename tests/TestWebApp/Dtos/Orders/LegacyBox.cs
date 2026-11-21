#nullable disable
namespace TestWebApp.Dtos.Orders;

// a type without nullable annotations, as an older package has
public class LegacyBox
{
    public Label Label { get; set; }
}
