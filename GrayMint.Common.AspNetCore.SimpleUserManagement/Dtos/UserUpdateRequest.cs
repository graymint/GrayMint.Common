using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class UserUpdateRequest<T>
{
    public Patch<string>? Email { get; set; }
    public Patch<string?>? FirstName { get; set; }
    public Patch<string?>? LastName { get; set; }
    public Patch<string?>? Description { get; set; }
    public Patch<bool>? IsBot { get; init; }
    public Patch<T?>? ExData { get; set; }
}


public class UserUpdateRequest : UserUpdateRequest<string>
{
}
