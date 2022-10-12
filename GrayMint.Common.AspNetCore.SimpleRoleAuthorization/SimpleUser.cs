namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleUser
{
    public string? AuthCode { get; set; }
    public SimpleUserRole[] UserRoles { get; set; } = Array.Empty<SimpleUserRole>();
}