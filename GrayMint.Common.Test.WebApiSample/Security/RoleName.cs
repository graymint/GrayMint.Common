namespace GrayMint.Common.Test.WebApiSample.Security;

public static class RoleName
{
    public const string SystemAdmin = nameof(SystemAdmin);
    public const string EnterpriseAdmin = "cognito:Enterprise_Admin";
    public const string AppReader = nameof(AppReader);
    public const string AppUser = nameof(AppUser);
}