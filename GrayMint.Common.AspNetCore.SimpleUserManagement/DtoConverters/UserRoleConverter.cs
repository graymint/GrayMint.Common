using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserRoleConverter
{
    public static UserRole ToDto(this UserRoleModel model)
    {
        var userRole = new UserRole
        {
            User = model.User?.ToDto() ?? throw new Exception("User has not been fetched."),
            Role = model.Role?.ToDto() ?? throw new Exception("Role has not been fetched."),
            ResourceId = model.ResourceId
        };
        return userRole;
    }
}


