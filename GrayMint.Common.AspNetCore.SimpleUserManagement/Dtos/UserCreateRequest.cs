namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserCreateRequest
{
    public string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; }

    public UserCreateRequest(string email)
    {
        Email = email;
    }
}