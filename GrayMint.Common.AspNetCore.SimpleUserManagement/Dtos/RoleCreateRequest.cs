namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class RoleCreateRequest
{
    public string RoleName { get; set; }
    public string? Description { get; set; }

    public RoleCreateRequest(string roleName)
    {
        RoleName = roleName;
    }
}