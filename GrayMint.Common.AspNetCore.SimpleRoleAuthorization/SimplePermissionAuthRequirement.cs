using Microsoft.AspNetCore.Authorization;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

internal class SimplePermissionAuthRequirement : IAuthorizationRequirement
{
    public string PermissionId { get; }
    public SimplePermissionAuthRequirement(string permissionId)
    {
        PermissionId = permissionId;
    }
}