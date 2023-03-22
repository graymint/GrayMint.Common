namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRolePermissions
{
    public string RoleName { get; }
    public string[] PermissionIds { get; }
    public SimpleRolePermissions(string roleName, IEnumerable<string> permissionIds)
    {
        RoleName = roleName;
        PermissionIds = permissionIds.ToArray();
    }
}