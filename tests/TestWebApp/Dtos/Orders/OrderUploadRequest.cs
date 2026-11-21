namespace TestWebApp.Dtos.Orders;

public class OrderUploadRequest
{
    public required IFormFile File { get; init; }
}
