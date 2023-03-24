using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserRoleConverter
{
    public static UserRole<T> ToDto<T>(this UserRoleModel model)
    {
        var userRole = new UserRole<T>
        {
            User = model.User?.ToDto<T>() ?? throw new Exception("User has not been fetched."),
            Role = model.Role?.ToDto() ?? throw new Exception("Role has not been fetched."),
            AppId = model.AppId
        };
        return userRole;
    }
}


