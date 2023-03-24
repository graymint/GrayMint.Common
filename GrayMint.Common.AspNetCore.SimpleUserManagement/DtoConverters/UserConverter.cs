using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;
using GrayMint.Common.Utils;
using System.Text.Json;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;

internal static class UserConverter
{
    public static string? ConvertExDataToString<T>(T? exData)
    {
        if (exData == null) return default;
        if (typeof(T) == typeof(string)) return exData.ToString();
        return JsonSerializer.Serialize(exData);
    }


    public static  T? ConvertExDataFromString<T>(string? exData)
    {
        if (exData == null) return default;
        if (typeof(T) == typeof(string)) return (T?)(object)exData;
        return GmUtil.JsonDeserialize<T>(exData);
    }

    public static User<T> ToDto<T>(this UserModel model)
    {
        var user = new User<T>
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
            ExData = ConvertExDataFromString<T>(model.ExData)
        };

        return user;
    }
}