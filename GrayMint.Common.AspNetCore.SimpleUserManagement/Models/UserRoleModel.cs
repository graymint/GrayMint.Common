namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

internal class UserRoleModel
{
    public Guid UserId { get; set; } 
    public Guid RoleId { get; set; }
    public string AppId { get; set; } = default!;
    public virtual RoleModel? Role { get; set; }
    public virtual UserModel? User { get; set; }
}