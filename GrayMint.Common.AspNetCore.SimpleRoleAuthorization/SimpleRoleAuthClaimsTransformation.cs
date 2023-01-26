using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

internal class SimpleRoleAuthClaimsTransformation : IClaimsTransformation
{
    private readonly SimpleUserResolver _authUserResolver;

    public SimpleRoleAuthClaimsTransformation(SimpleUserResolver authUserResolver)
    {
        _authUserResolver = authUserResolver;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // convert standard role claims to app-role claims
        var claimsIdentity = new ClaimsIdentity();
        foreach (var claim in principal.Claims.Where(x => x.Type == ClaimTypes.Role))
            claimsIdentity.AddClaim(SimpleRoleAuth.CreateAppRoleClaim(claim.Value, "*"));

        // add simple roles to app-role claims
        var authUser = await _authUserResolver.GetSimpleAuthUser(principal);
        if (authUser?.UserRoles != null)
        {

            // Add the following claims
            // RoleName/apps/*
            // RoleName/apps/appId
            foreach (var userRole in authUser.UserRoles)
            {
                claimsIdentity.AddClaim(SimpleRoleAuth.CreateAppRoleClaim(userRole.RoleName, userRole.AppId));
                if (!claimsIdentity.HasClaim(x => x.Type == ClaimTypes.Role && x.Value == userRole.RoleName))
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, userRole.RoleName));
            }
        }

        principal.AddIdentity(claimsIdentity);
        return principal;
    }
}