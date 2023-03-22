using Microsoft.Extensions.Caching.Memory;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthCache
{
    private readonly IMemoryCache _memoryCache;

    private static string GetUserCacheKey(string nameIdentifier) => $"SimpleAuthUser:{nameIdentifier}";

    public SimpleRoleAuthCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task ClearUserCache(string userEmail)
    {
        _memoryCache.Remove(GetUserCacheKey(userEmail));
        return Task.CompletedTask;
    }

    public bool TryGetSimpleUserByEmail(string email, out SimpleUser? simpleUser)
    {
        return _memoryCache.TryGetValue(GetUserCacheKey(email), out simpleUser);
    }

    public void Set(string email, SimpleUser? simpleUser)
    {
        _memoryCache.Set(email, simpleUser);
    }
}