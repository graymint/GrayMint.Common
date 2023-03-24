namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserRole<T>
{
    public required User<T> User { get; set; }
    public required Role Role { get; set; }
    public required string? AppId { get; set; }
}

public class UserRole : UserRole<string>
{
}

