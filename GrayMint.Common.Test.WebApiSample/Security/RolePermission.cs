using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

namespace GrayMint.Common.Test.WebApiSample.Security;

public static class RolePermission
{
    public static SimpleRolePermissions AppReader = new(
        nameof(AppReader),
        new[]
        {
            nameof(Permission.ItemRead)
        }
    );

    public static SimpleRolePermissions AppUser = new(
        nameof(AppUser),
        new[]
        {
            nameof(Permission.ItemWrite),
        }.Concat(AppReader.PermissionIds)

    );

    public static SimpleRolePermissions SystemAdmin = new(
        nameof(SystemAdmin),
        new[]
        {
            nameof(Permission.SystemWrite),
            nameof(Permission.SystemRead),
        }.Concat(AppUser.PermissionIds)
    );

    public static SimpleRolePermissions EnterpriseAdmin = new(
        "cognito:Enterprise_Admin", SystemAdmin.PermissionIds
    );

    public static SimpleRolePermissions[] All =
    {
        SystemAdmin,
        EnterpriseAdmin,
        AppUser,
        AppReader
    };
}