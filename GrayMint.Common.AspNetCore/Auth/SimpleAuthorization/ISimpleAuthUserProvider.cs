using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public interface ISimpleAuthUserProvider
{
    Task<SimpleAuthUser?> GetAuthUser(string email);
}   