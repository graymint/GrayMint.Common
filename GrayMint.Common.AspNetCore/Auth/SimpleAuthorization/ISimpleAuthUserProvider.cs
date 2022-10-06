namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public interface ISimpleAuthUserProvider
{
    Task ResetAuthCodeByEmail(string email);
    Task<SimpleAuthUser?> GetAuthUserByEmail(string email);
}   