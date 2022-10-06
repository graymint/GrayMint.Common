using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

internal class SimpleAuthUserResolver : IBotAuthenticationProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly ISimpleAuthUserProvider _simpleUserProvider;
    private readonly AppAuthSettings _appAuthSettings;

    public SimpleAuthUserResolver(IMemoryCache memoryCache,
        ISimpleAuthUserProvider simpleUserProvider,
        IOptions<AppAuthSettings> appAuthSettings)
    {
        _memoryCache = memoryCache;
        _simpleUserProvider = simpleUserProvider;
        _appAuthSettings = appAuthSettings.Value;
    }

    public async Task<SimpleAuthUser?> GetSimpleAuthUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // try to find from cache
        var cacheKey = userId != null ? $"SimpleAuthUser:{userId}" : null;
        if (cacheKey != null && _memoryCache.TryGetValue(cacheKey, out SimpleAuthUser? userAuthInfo))
            return userAuthInfo;

        // add to cache
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        try
        {
            userAuthInfo = string.IsNullOrEmpty(email) ? null : await _simpleUserProvider.GetAuthUserByEmail(email);
        }
        catch (Exception ex) when(NotExistsException.Is(ex))
        {
            userAuthInfo = null;
        }

        if (cacheKey != null)
            _memoryCache.Set(cacheKey, userAuthInfo, _appAuthSettings.CacheTimeout);

        return userAuthInfo;
    }

    public async Task<string> GetAuthCode(ClaimsPrincipal principal)
    {
        var authUser = await GetSimpleAuthUser(principal);
        return authUser?.AuthCode ?? throw new KeyNotFoundException("User does not exist.");
    }
}