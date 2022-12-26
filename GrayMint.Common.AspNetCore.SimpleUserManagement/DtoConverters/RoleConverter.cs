using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConveters;

public static class RoleConveter
{
    public static Role ToDto(this Models.Role roleModel)
    {
        var role = new Role(roleModel.RoleId.ToString(), roleModel.RoleName)
        {
            Description = roleModel.Description
        };

        return role;
    }
}


