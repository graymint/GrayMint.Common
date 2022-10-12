namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public interface ISimpleRoleAuthUserProvider
{
    Task ResetAuthCodeByEmail(string email);
    Task<SimpleUser?> GetAuthUserByEmail(string email);
}