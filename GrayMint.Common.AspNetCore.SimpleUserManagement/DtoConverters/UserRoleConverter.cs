using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

public static class UserRoleConveter
{
    public static UserRole ToDto(this Models.UserRole userRoleModel)
    {
        if (userRoleModel.Role == null)
            throw new Exception("Role has not been fetched.");

        var userRole = new UserRole(userRoleModel.Role.ToDto())
        {
            AppId = userRoleModel.AppId
        };
        return userRole;
    }
}


