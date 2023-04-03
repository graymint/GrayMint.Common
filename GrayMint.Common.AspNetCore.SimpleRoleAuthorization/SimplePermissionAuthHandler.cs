using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

internal class SimplePermissionAuthHandler : AuthorizationHandler<SimplePermissionAuthRequirement>
{
    private readonly SimpleRoleAuthOptions _options;

    public SimplePermissionAuthHandler(IOptions<SimpleRoleAuthOptions> options)
    {
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SimplePermissionAuthRequirement requirement)
    {
        string? requestAppId;
        if (context.Resource is SimpleRoleResource roleResource)
            requestAppId = roleResource.Resource;
        else if (context.Resource is HttpContext httpContext)
            requestAppId = httpContext.GetRouteValue(_options.ResourceParamName)?.ToString();
        else
            return Task.CompletedTask;

        var succeeded = false;

        succeeded |= context.User.HasClaim(
            SimpleRoleAuth.PermissionClaimType, 
            SimpleRoleAuth.CreateAppPermission("*", requirement.PermissionId));

        succeeded |= requestAppId != null &&
                     context.User.HasClaim(
                         SimpleRoleAuth.PermissionClaimType, 
                         SimpleRoleAuth.CreateAppPermission(requestAppId, requirement.PermissionId));

        if (succeeded)
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this, "Access forbidden."));

        return Task.CompletedTask;
    }
}