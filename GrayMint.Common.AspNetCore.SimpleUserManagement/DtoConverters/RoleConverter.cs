using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

public static class RoleConverter
{
    public static Role ToDto(this Models.Role roleModel)
    {
        var role = new Role(roleModel.RoleId, roleModel.RoleName)
        {
            Description = roleModel.Description
        };

        return role;
    }
}


