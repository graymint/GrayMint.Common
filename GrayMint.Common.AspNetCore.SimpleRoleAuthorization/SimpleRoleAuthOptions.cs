namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthOptions
{
    public string ResourceParamName { get; set; } = "appId";
    public string? CustomAuthenticationScheme { get; set; }
    public SimpleRole[]? Roles { get; set; }
}