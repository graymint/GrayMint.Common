namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

internal class RoleModel
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = default!;

    public virtual ICollection<UserRoleModel>? UserRoles { get; set; }
    public string? Description { get; set; }
}