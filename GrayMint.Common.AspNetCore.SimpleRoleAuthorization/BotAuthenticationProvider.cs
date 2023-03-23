using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class BotAuthenticationProvider : IBotAuthenticationProvider
{
    private readonly ISimpleUserProvider _simpleUserProvider;

    public BotAuthenticationProvider(ISimpleUserProvider simpleUserProvider)
    {
        _simpleUserProvider = simpleUserProvider;
    }

    public async Task<string> GetAuthorizationCode(ClaimsPrincipal principal)
    {
        var simpleUser = await _simpleUserProvider.FindSimpleUser(principal);
        return simpleUser?.AuthorizationCode ?? throw new KeyNotFoundException("User does not exist.");
    }
}