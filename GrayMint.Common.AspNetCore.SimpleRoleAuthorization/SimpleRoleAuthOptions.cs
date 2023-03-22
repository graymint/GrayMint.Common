namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthOptions
{
    public string AppIdParamName { get; set; } = "appId";
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public string? CustomAuthenticationScheme { get; set; }
    public SimpleRole[]? RolePermissions { get; set; }
   
}