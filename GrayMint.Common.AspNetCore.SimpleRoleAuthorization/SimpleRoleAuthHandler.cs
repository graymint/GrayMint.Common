using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthHandler : AuthorizationHandler<SimpleRoleAuthRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SimpleRoleAuthRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var requestAppId = httpContext.GetRouteValue("appId")?.ToString();
        var requiredRoles = context.Requirements.OfType<RolesAuthorizationRequirement>().ToArray();

        foreach (var requiredRole in requiredRoles)
        {
            var succeeded = false;
            foreach (var allowedRole in requiredRole.AllowedRoles)
            {
                succeeded |= context.User.HasClaim(x =>
                        x.Type == SimpleRoleAuth.RoleClaimType &&
                        x.Value == SimpleRoleAuth.CreateAppRoleName(allowedRole, "*"));

                succeeded |= requestAppId != null &&
                             context.User.HasClaim(x =>
                                 x.Type == SimpleRoleAuth.RoleClaimType &&
                                 x.Value == SimpleRoleAuth.CreateAppRoleName(allowedRole, requestAppId));
            }

            if (!succeeded)
            {
                context.Fail(new AuthorizationFailureReason(this, "Access forbidden."));
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}