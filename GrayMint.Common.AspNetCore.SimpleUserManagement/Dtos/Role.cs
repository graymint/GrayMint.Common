
namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class Role
{
    public string RoleId { get; set; } 
    public string RoleName { get; set; }
    public string? Description { get; set; }

    public Role(string roleId, string roleName)
    {
        RoleId = roleId;
        RoleName = roleName;
    }

}