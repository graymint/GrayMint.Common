using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

namespace GrayMint.Common.Test.WebApiSample.Security;

public static class Roles
{
    public static SimpleRole AppReader { get; } = new(
        nameof(AppReader),
        new[]
        {
            nameof(Permission.ItemRead)
        }
    );

    public static SimpleRole AppUser { get; } = new(
        nameof(AppUser),
        new[]
        {
            nameof(Permission.ItemWrite),
        }.Concat(AppReader.Permissions)
    );

    public static SimpleRole SystemAdmin { get; } = new(
        nameof(SystemAdmin),
        new[]
        {
            nameof(Permission.SystemWrite),
            nameof(Permission.SystemRead),
        }.Concat(AppUser.Permissions)
    );

    public static SimpleRole EnterpriseAdmin { get; } = new(
        "cognito:Enterprise_Admin", SystemAdmin.Permissions
    );

    public static SimpleRole[] All { get; } =
    {
        SystemAdmin,
        EnterpriseAdmin,
        AppUser,
        AppReader
    };
}