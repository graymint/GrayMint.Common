using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

namespace GrayMint.Common.Test.WebApiSample.Security;

public static class RolePermission
{
    public static SimpleRolePermissions AppReader = new(
        RoleName.AppReader, new[]
        {
            nameof(Permission.ReadItem)
        }
    );

    public static SimpleRolePermissions AppUser = new(
        RoleName.AppUser, new[]
        {
            nameof(Permission.CreateItem),
            nameof(Permission.DeleteItem),
        }.Concat(AppReader.PermissionIds)

    );


    public static SimpleRolePermissions SystemAdmin = new(
        RoleName.SystemAdmin, new[]
        {
            nameof(Permission.CreateApp),
        }.Concat(AppUser.PermissionIds)
    );

    public static SimpleRolePermissions EnterpriseAdmin = new(
        RoleName.EnterpriseAdmin, SystemAdmin.PermissionIds
    );

    public static SimpleRolePermissions[] All = { SystemAdmin, EnterpriseAdmin, AppUser, AppReader };
}