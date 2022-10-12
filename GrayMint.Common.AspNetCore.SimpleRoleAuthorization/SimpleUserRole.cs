namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleUserRole
{
    public string RoleName { get; set; }
    public string AppId { get; set; }

    public SimpleUserRole(string roleName, string appId)
    {
        RoleName = roleName;
        AppId = appId;
    }
}