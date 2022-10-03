namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;

    public virtual ICollection<UserRole>? UserRoles { get; set; }
    public string? Description { get; set; }
}