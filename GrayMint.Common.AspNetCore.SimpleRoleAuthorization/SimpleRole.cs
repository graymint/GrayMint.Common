namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRole
{
    public string RoleName { get; }
    public string[] Permissions { get; }
    public SimpleRole(string roleName, IEnumerable<string> permissions)
    {
        RoleName = roleName;
        Permissions = permissions.ToArray();
    }
}