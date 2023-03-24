namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthOptions
{
    public string AppIdParamName { get; set; } = "appId";
    public string? CustomAuthenticationScheme { get; set; }
    public SimpleRole[]? Roles { get; set; }
}