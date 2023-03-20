
namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class Role
{
    public Guid RoleId { get; set; } 
    public string RoleName { get; set; }
    public string? Description { get; set; }

    public Role(Guid roleId, string roleName)
    {
        RoleId = roleId;
        RoleName = roleName;
    }

}