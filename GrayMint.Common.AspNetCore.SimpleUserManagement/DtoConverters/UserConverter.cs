using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserConverter
{
    public static User ToDto(this UserModel model)
    {
        var user = new User
        {
            UserId = model.UserId,
            Email = model.Email,
            CreatedTime = model.CreatedTime,
            AuthCode = model.AuthCode,
            FirstName = model.FirstName,
            LastName = model.LastName,
            AccessedTime = model.AccessedTime,
            Description = model.Description,
            IsBot = model.IsBot,
            ExData = model.ExData
        };

        return user;
    }
}