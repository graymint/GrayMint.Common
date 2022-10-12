using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuth
{
    public const string Policy = "SimpleRolePolicy";
    public const string RoleClaimType = "app-role";

    public static string CreateAppRoleName(string roleName, string appId)
    {
        return $"{roleName}/apps/{appId}";
    }

    public static Claim CreateAppRoleClaim(string roleName, string appId)
    {
        return new Claim(RoleClaimType, CreateAppRoleName(roleName, appId));
    }
}