namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedTime { get; set; }
    public string? AuthCode { get; set; }

    public User(Guid userId, string email, DateTime createdTime)
    {
        UserId = userId;
        Email = email;
        CreatedTime = createdTime;
    }
}