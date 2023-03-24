
namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class Role
{
    public required Guid RoleId { get; set; } 
    public required string RoleName { get; set; }
    public required string? Description { get; set; }
}