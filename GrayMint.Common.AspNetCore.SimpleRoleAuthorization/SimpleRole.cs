namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRole
{
    public string RoleName { get; }
    public string[] PermissionIds { get; }
    public SimpleRole(string roleName, IEnumerable<string> permissionIds)
    {
        RoleName = roleName;
        PermissionIds = permissionIds.ToArray();
    }
}