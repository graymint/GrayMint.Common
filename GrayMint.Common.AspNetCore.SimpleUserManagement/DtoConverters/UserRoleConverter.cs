using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserRoleConverter
{
    public static UserRole ToDto(this Models.UserRoleModel userRoleModelModel)
    {
        if (userRoleModelModel.Role == null)
            throw new Exception("Role has not been fetched.");

        var userRole = new UserRole(userRoleModelModel.Role.ToDto())
        {
            AppId = userRoleModelModel.AppId
        };
        return userRole;
    }
}


