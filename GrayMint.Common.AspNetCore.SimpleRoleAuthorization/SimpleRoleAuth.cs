using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuth
{
    public const string Policy = "SimpleRolePolicy";
    public const string RoleClaimType = "app-role";
    public const string PermissionClaimType = "app-permission";

    public static string CreateAppRoleName(string appId, string roleName)
    {
        return $"/apps/{appId}/roles/{roleName}";
    }

    public static Claim CreateAppRoleClaim(string appId, string roleName)
    {
        return new Claim(RoleClaimType, CreateAppRoleName(appId, roleName));
    }

    public static string CreateAppPermission(string appId, string permissionId)
    {
        return $"/apps/{appId}/permissions/{permissionId}";
    }

    public static Claim CreateAppPermissionClaim(string appId, string permissionId)
    {
        return new Claim(PermissionClaimType, CreateAppPermission(appId, permissionId));
    }

    public static string CreatePolicyNameForPermission(string permissionId)
    {
        return $"Has{permissionId}Policy";
    }
}