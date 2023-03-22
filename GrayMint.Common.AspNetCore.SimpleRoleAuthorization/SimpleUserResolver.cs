using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.Exceptions;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleUserResolver : IBotAuthenticationProvider
{
    private readonly SimpleRoleAuthCache _simpleRoleAuthCache;
    private readonly ISimpleUserProvider _simpleUserProvider;

    public SimpleUserResolver(
        ISimpleUserProvider simpleUserProvider,
        SimpleRoleAuthCache simpleRoleAuthCache)
    {
        _simpleUserProvider = simpleUserProvider;
        _simpleRoleAuthCache = simpleRoleAuthCache;
    }

    public async Task<SimpleUser?> GetSimpleAuthUser(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (email == null) return null;

        // try to find from cache
        if (_simpleRoleAuthCache.TryGetSimpleUserByEmail(email, out var simpleUser))
            return simpleUser;

        // add to cache
        try
        {
            simpleUser = await _simpleUserProvider.FindSimpleUserByEmail(email);
        }
        catch (Exception ex) when (NotExistsException.Is(ex))
        {
        }

        _simpleRoleAuthCache.Set(email, simpleUser);
        return simpleUser;
    }

    public async Task<string> GetAuthorizationCode(ClaimsPrincipal principal)
    {
        var authUser = await GetSimpleAuthUser(principal);
        return authUser?.AuthorizationCode ?? throw new KeyNotFoundException("User does not exist.");
    }

    public Task ClearUserCache(string email)
    {
        _simpleRoleAuthCache.ClearUserCache(email);
        return Task.CompletedTask;
    }
}