namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

public class User
{
    public Guid UserId { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedTime { get; set; }
    public string? AuthCode { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<UserRole>? UserRoles { get; set; }
}