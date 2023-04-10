namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserRole
{
    public required string ResourceId { get; set; }
    public required User User { get; set; }
    public required Role Role { get; set; }
}
