using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

internal class SimpleUserResolver : IBotAuthenticationProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly ISimpleRoleAuthUserProvider _simpleUserProvider;
    private readonly SimpleRoleAuthOptions _simpleRoleAuthOptions;

    public SimpleUserResolver(IMemoryCache memoryCache,
        ISimpleRoleAuthUserProvider simpleUserProvider,
        IOptions<SimpleRoleAuthOptions> simpleRoleAuthOptions)
    {
        _memoryCache = memoryCache;
        _simpleUserProvider = simpleUserProvider;
        _simpleRoleAuthOptions = simpleRoleAuthOptions.Value;
    }

    public async Task<SimpleUser?> GetSimpleAuthUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // try to find from cache
        var cacheKey = userId != null ? $"SimpleAuthUser:{userId}" : null;
        if (cacheKey != null && _memoryCache.TryGetValue(cacheKey, out SimpleUser? userAuthInfo))
            return userAuthInfo;

        // add to cache
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        try
        {
            userAuthInfo = string.IsNullOrEmpty(email) ? null : await _simpleUserProvider.GetAuthUserByEmail(email);
        }
        catch (Exception ex) when (NotExistsException.Is(ex))
        {
            userAuthInfo = null;
        }

        if (cacheKey != null)
            _memoryCache.Set(cacheKey, userAuthInfo, _simpleRoleAuthOptions.CacheTimeout);

        return userAuthInfo;
    }

    public async Task<string> GetAuthorizationCode(ClaimsPrincipal principal)
    {
        var authUser = await GetSimpleAuthUser(principal);
        return authUser?.AuthCode ?? throw new KeyNotFoundException("User does not exist.");
    }
}