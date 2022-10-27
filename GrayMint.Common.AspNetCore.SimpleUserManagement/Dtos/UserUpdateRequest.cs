using GrayMint.Common.Client;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserUpdateRequest
{
    public Patch<string>? Email { get; set; }
    public Patch<string?>? FirstName { get; set; }
    public Patch<string?>? LastName { get; set; }
    public Patch<string?>? Description { get; set; }
}