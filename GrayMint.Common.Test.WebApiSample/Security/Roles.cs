using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

namespace GrayMint.Common.Test.WebApiSample.Security;

public static class Roles
{
    public static SimpleRole AppReader { get; } = new(
        nameof(AppReader),
        Guid.Parse("{C7383857-4513-4FE5-BC0D-6DEC069FCF1E}"),
        new[]
        {
            nameof(Permissions.ItemRead)
        }
    );

    public static SimpleRole AppUser { get; } = new(
        nameof(AppUser),
        Guid.Parse("{114FDE8C-55C5-44EE-A008-9069C21CD129}"),
        new[]
        {
            nameof(Permissions.ItemWrite),
        }.Concat(AppReader.Permissions)
    );

    public static SimpleRole SystemAdmin { get; } = new(
        nameof(SystemAdmin),
        Guid.Parse("{AC3A840C-1DDF-4D88-890F-6713DD8F0DDE}"),
        new[]
        {
            nameof(Permissions.SystemWrite),
            nameof(Permissions.SystemRead),
        }.Concat(AppUser.Permissions)
    );

    public static SimpleRole EnterpriseAdmin { get; } = new(
        "cognito:Enterprise_Admin",
        Guid.Parse("{4D79F619-319B-4787-BCEE-FD0DDF3EE75A}"),
        SystemAdmin.Permissions
    );

    public static SimpleRole[] All { get; } =
    {
        SystemAdmin,
        EnterpriseAdmin,
        AppUser,
        AppReader
    };
}