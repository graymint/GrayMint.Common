namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

internal class UserRoleModel
{
    public string ResourceId { get; set; } = default!;
    public Guid UserId { get; set; } 
    public Guid RoleId { get; set; }
    public virtual RoleModel? Role { get; set; }
    public virtual UserModel? User { get; set; }
}