using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class RoleConverter
{
    public static Role ToDto(this RoleModel model)
    {
        var role = new Role
        {
            RoleId = model.RoleId,
            RoleName = model.RoleName,
            Description = model.Description
        };

        return role;
    }
}


