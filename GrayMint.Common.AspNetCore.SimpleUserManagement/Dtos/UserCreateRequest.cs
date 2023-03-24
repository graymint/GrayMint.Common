namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserCreateRequest<T>
{
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Description { get; init; }
    public T? ExData { get; init; }
    public string? Custom2 { get; init; }
}

public class UserCreateRequest : UserCreateRequest<string>
{
}