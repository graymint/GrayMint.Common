namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleUser
{
    public required string UserId { get; init; }
    public required SimpleUserRole[] UserRoles { get; init; }
    public required string? AuthorizationCode { get; init; }
}