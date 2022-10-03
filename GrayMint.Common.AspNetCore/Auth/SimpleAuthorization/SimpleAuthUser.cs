namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public class SimpleAuthUser
{
    public string? AuthCode { get; set; }
    public SimpleAuthUserRole[] UserRoles { get; set; } = Array.Empty<SimpleAuthUserRole>();
}