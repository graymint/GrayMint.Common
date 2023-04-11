namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;

public class UserApiKey
{
    public required Guid UserId { get; set; }
    public required string Authorization { get; init; }
}