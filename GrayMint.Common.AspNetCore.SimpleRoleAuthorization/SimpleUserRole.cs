namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleUserRole
{
    public string RoleName { get; set; }
    public string ResourceId { get; set; }

    public SimpleUserRole(string roleName, string resourceId)
    {
        RoleName = roleName;
        ResourceId = resourceId;
    }
}