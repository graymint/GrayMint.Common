using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

namespace GrayMint.Common.Test.WebApiSample.Security;

public static class Roles
{
    public static SimpleRole AppReader = new(
        nameof(AppReader),
        new[]
        {
            nameof(Permission.ItemRead)
        }
    );

    public static SimpleRole AppUser = new(
        nameof(AppUser),
        new[]
        {
            nameof(Permission.ItemWrite),
        }.Concat(AppReader.PermissionIds)

    );

    public static SimpleRole SystemAdmin = new(
        nameof(SystemAdmin),
        new[]
        {
            nameof(Permission.SystemWrite),
            nameof(Permission.SystemRead),
        }.Concat(AppUser.PermissionIds)
    );

    public static SimpleRole EnterpriseAdmin = new(
        "cognito:Enterprise_Admin", SystemAdmin.PermissionIds
    );

    public static SimpleRole[] All =
    {
        SystemAdmin,
        EnterpriseAdmin,
        AppUser,
        AppReader
    };
}