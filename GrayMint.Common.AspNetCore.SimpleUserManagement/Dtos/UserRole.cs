namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserRole
{
    public UserRole(Role role)
    {
        Role = role;
    }

    public Role Role { get; set; }
    public string? AppId { get; set; }
}