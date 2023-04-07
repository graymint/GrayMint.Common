namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;

public class ApiKeyResult
{
    public required Guid UserId { get; set; }
    public required string Authorization { get; init; }
}