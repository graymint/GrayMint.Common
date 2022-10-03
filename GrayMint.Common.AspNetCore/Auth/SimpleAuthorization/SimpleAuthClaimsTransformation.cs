using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

internal class SimpleAuthClaimsTransformation : IClaimsTransformation
{
    private readonly SimpleAuthUserResolver _authUserResolver;
    private readonly IOptions<SimpleAuthOptions> _simpleAuthOptions;

    public SimpleAuthClaimsTransformation(SimpleAuthUserResolver authUserResolver, IOptions<SimpleAuthOptions> simpleAuthOptions)
    {
        _authUserResolver = authUserResolver;
        _simpleAuthOptions = simpleAuthOptions;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // convert standard role claims to app-role claims
        var claimsIdentity = new ClaimsIdentity();
        foreach (var claim in principal.Claims.Where(x => x.Type == ClaimTypes.Role))
            claimsIdentity.AddClaim(SimpleAuth.CreateAppRoleClaim(claim.Value, "*"));

        // add simple roles to app-role claims
        var authUser = await _authUserResolver.GetSimpleAuthUser(principal);
        if (authUser?.UserRoles != null)
        {

            // Add the following claims
            // RoleName/apps/*
            // RoleName/apps/appId
            foreach (var userRole in authUser.UserRoles)
            {
                claimsIdentity.AddClaim(SimpleAuth.CreateAppRoleClaim(userRole.RoleName, userRole.AppId));
                if (!claimsIdentity.HasClaim(x => x.Type == ClaimTypes.Role && x.Value == userRole.RoleName))
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, userRole.RoleName));
            }
        }

        principal.AddIdentity(claimsIdentity);
        return principal;
    }
}