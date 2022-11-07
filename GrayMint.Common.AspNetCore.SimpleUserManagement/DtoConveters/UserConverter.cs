using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConveters;

public static class UserConverter
{
    public static User ToDto(this Models.User userModel)
    {
        var user = new User(userModel.UserId.ToString(), email: userModel.Email, createdTime: userModel.CreatedTime)
        {
            AuthCode = userModel.AuthCode,
            FirstName = userModel.FirstName,
            LastName = userModel.LastName,
            Description = userModel.Description,
        };

        return user;
    }
}