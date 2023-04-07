using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;

public class TeamUpdateUserParam
{
    public Patch<Guid>? RoleId { get; set; }
}