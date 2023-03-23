using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class RoleConverter
{
    public static Role ToDto(this Models.RoleModel roleModelModel)
    {
        var role = new Role(roleModelModel.RoleId, roleModelModel.RoleName)
        {
            Description = roleModelModel.Description
        };

        return role;
    }
}


