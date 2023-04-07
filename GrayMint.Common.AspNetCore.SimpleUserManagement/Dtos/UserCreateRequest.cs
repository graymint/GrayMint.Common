namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserCreateRequest
{
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Description { get; init; }
    public string? ExData { get; init; }
    public bool IsBot { get; init; }
}
