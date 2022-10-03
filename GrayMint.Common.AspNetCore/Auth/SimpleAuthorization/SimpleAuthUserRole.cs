namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public class SimpleAuthUserRole
{
    public string RoleName { get; set; }
    public string AppId { get; set; }

    public SimpleAuthUserRole(string roleName, string appId)
    {
        RoleName = roleName;
        AppId = appId;
    }
}