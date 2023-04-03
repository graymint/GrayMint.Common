using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthService
{
    private readonly IAuthorizationService _authorizationService;

    public SimpleRoleAuthService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public Task<AuthorizationResult> AuthorizePermissionAsync(ClaimsPrincipal user, string? resource, string permission)
    {
        return _authorizationService.AuthorizeAsync(user, new SimpleRoleResource(resource), SimpleRoleAuth.CreatePolicyNameForPermission(permission));
    }
}