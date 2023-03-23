using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserConverter
{
    public static User ToDto(this Models.UserModel userModelModel)
    {
        var user = new User(userModelModel.UserId, email: userModelModel.Email, createdTime: userModelModel.CreatedTime)
        {
            AuthCode = userModelModel.AuthCode,
            FirstName = userModelModel.FirstName,
            LastName = userModelModel.LastName,
            Description = userModelModel.Description
        };

        return user;
    }
}