using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public interface IBotAuthenticationProvider
{
    public Task<string> GetAuthorizationCode(ClaimsPrincipal principal);
}