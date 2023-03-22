using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

internal class SimpleRoleAuthClaimsTransformation : IClaimsTransformation
{
    private readonly SimpleUserResolver _simpleUserResolver;
    private readonly SimpleRoleAuthOptions _roleAuthOptions;

    public SimpleRoleAuthClaimsTransformation(
        SimpleUserResolver simpleUserResolver,
        IOptions<SimpleRoleAuthOptions> roleAuthOptions)
    {
        _simpleUserResolver = simpleUserResolver;
        _roleAuthOptions = roleAuthOptions.Value;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var simpleRolePermissions = _roleAuthOptions.Roles ?? Array.Empty<SimpleRole>();

        // convert standard role claims to app-role claims
        var claimsIdentity = new ClaimsIdentity();
        foreach (var claim in principal.Claims.Where(x => x.Type == ClaimTypes.Role))
        {
            claimsIdentity.AddClaim(SimpleRoleAuth.CreateAppRoleClaim("*", claim.Value));
            AddPermissionClaims(claimsIdentity, "*", claim.Value, simpleRolePermissions);
        }

        // add simple roles to app-role claims
        var authUser = await _simpleUserResolver.GetSimpleAuthUser(principal);
        if (authUser?.UserRoles != null)
        {

            // Add the following claims
            // /apps/*/RoleName
            // /apps/appId/RoleName
            foreach (var userRole in authUser.UserRoles)
            {
                claimsIdentity.AddClaim(SimpleRoleAuth.CreateAppRoleClaim(userRole.AppId, userRole.RoleName));
                AddPermissionClaims(claimsIdentity, userRole.AppId, userRole.RoleName, simpleRolePermissions);

                // add standard claim role
                if (userRole.AppId == "*" && !claimsIdentity.HasClaim(ClaimTypes.Role, userRole.RoleName))
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, userRole.RoleName));
            }
        }

        principal.AddIdentity(claimsIdentity);
        return principal;
    }

    private static void AddPermissionClaims(ClaimsIdentity claimsIdentity, string appId, string roleName,
        IEnumerable<SimpleRole> rolePermissions)
    {
        var rolePermission = rolePermissions.FirstOrDefault(x => x.RoleName == roleName);
        if (rolePermission == null)
            return;

        foreach (var permissionId in rolePermission.Permissions)
        {
            var claim = SimpleRoleAuth.CreateAppPermissionClaim(appId, permissionId);
            if (!claimsIdentity.HasClaim(claim.Type, claim.Value))
                claimsIdentity.AddClaim(claim);
        }
    }
}