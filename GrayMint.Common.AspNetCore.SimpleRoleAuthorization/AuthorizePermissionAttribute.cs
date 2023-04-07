using Microsoft.AspNetCore.Authorization;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public AuthorizePermissionAttribute(string permission)
        : base(SimpleRoleAuth.CreatePolicyNameForPermission(permission))
    {
    }

    
}