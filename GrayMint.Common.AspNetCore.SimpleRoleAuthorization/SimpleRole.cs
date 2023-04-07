namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRole
{
    public required string RoleName { get; init;}
    public required Guid RoleId { get; init; }
    public required string[] Permissions { get; init; }
    public required bool IsSystem { get; init; }
}