using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

public class RoleUpdateRequest
{
    public Patch<string>? RoleName { get; set; }
    public Patch<string?>? Description { get; set; }
}