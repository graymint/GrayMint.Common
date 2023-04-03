using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    private readonly SimpleRoleAuthOptions _simpleRoleAuthOptions;

    public SimpleRoleAuthHandler(IOptions<SimpleRoleAuthOptions> simpleRoleAuthOptions)
    {
        _simpleRoleAuthOptions = simpleRoleAuthOptions.Value;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var requestAppId = httpContext.GetRouteValue(_simpleRoleAuthOptions.ResourceParamName)?.ToString();

        var succeeded = false;
        foreach (var allowedRole in requirement.AllowedRoles)
        {
            succeeded |= context.User.HasClaim(x =>
                    x.Type == SimpleRoleAuth.RoleClaimType &&
                    x.Value == SimpleRoleAuth.CreateAppRoleName("*", allowedRole));

            succeeded |= requestAppId != null &&
                         context.User.HasClaim(x =>
                             x.Type == SimpleRoleAuth.RoleClaimType &&
                             x.Value == SimpleRoleAuth.CreateAppRoleName(requestAppId, allowedRole));
        }

        if (succeeded)
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this, "Access forbidden."));

        return Task.CompletedTask;
    }
}