using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuth
{
    public const string Policy = "SimpleRolePolicy";
    public const string RoleClaimType = "app-role";
    public const string PermissionClaimType = "app-permission";

    public static string CreateRoleName(string resourceId, string roleName)
    {
        return $"/resources/{resourceId}/roles/{roleName}".ToLower();
    }

    public static Claim CreateRoleClaim(string resourceId, string roleName)
    {
        return new Claim(RoleClaimType, CreateRoleName(resourceId, roleName));
    }

    public static string CreatePermission(string resourceId, string permissionId)
    {
        return $"/resources/{resourceId}/permissions/{permissionId}".ToLower();
    }

    public static Claim CreatePermissionClaim(string resourceId, string permissionId)
    {
        return new Claim(PermissionClaimType, CreatePermission(resourceId, permissionId));
    }

    public static string CreatePolicyNameForPermission(string permissionId)
    {
        return $"Has{permissionId}Policy";
    }
}