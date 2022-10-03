using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public class SimpleAuthHandler : AuthorizationHandler<SimpleAuthRequirement>
{
    private readonly ISimpleAuthUserProvider _simpleUserProvider;
    private readonly AppCommonSettings _appCommonSettings;

    public SimpleAuthHandler(ISimpleAuthUserProvider simpleUserProvider, IOptions<AppCommonSettings> appCommonSettings)
    {
        _simpleUserProvider = simpleUserProvider;
        _appCommonSettings = appCommonSettings.Value;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        SimpleAuthRequirement requirement)
    {
        try
        {
            await HandleRequirementImplAsync(context, requirement);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Fail(new AuthorizationFailureReason(this, ex.Message));
        }
    }

    private Task HandleRequirementImplAsync(AuthorizationHandlerContext context, SimpleAuthRequirement requirement)
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
                        x.Type == SimpleAuth.RoleClaimType &&
                        x.Value == SimpleAuth.CreateAppRoleName(allowedRole, "*"));

                succeeded |= requestAppId != null &&
                             context.User.HasClaim(x =>
                                 x.Type == SimpleAuth.RoleClaimType &&
                                 x.Value == SimpleAuth.CreateAppRoleName(allowedRole, requestAppId));
            }
            if (!succeeded)
                throw new UnauthorizedAccessException();
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}