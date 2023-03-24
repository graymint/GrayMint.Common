namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRole
{
    public string RoleName { get; }
    public Guid RoleId { get; }
    public string[] Permissions { get; }
    public SimpleRole(string roleName, Guid roleId, IEnumerable<string> permissions)
    {
        RoleName = roleName;
        RoleId = roleId;
        Permissions = permissions.ToArray();
    }
}