using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using GrayMint.Common.Utils;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserProvider : ISimpleUserProvider
{
    private readonly SimpleUserOptions _simpleUserOptions;
    private readonly SimpleUserDbContext _simpleUserDbContext;
    private readonly IMemoryCache _memoryCache;

    public SimpleUserProvider(
        SimpleUserDbContext simpleUserDbContext,
        IMemoryCache memoryCache,
        IOptions<SimpleUserOptions> simpleUserOptions)
    {
        _simpleUserDbContext = simpleUserDbContext;
        _memoryCache = memoryCache;
        _simpleUserOptions = simpleUserOptions.Value;
    }

    private static string GetUserCacheKey(string email) => $"SimpleAuthUser:{email}";

    public async Task<SimpleUser?> FindSimpleUser(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        if (email == null) return null;

        // check cache
        if (_memoryCache.TryGetValue(GetUserCacheKey(email), out SimpleUser? simpleUser))
            return simpleUser;

        var user = await _simpleUserDbContext.Users
            .Include(x => x.UserRoles)!
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == email);

        // not found
        if (user == null)
            return null;

        // update info by claims
        var givenName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName);
        var surnameName = claimsPrincipal.FindFirstValue(ClaimTypes.Surname);
        if (givenName != null) user.FirstName = givenName;
        if (surnameName != null) user.LastName = surnameName;
        if (user.AccessedTime is null || user.AccessedTime < DateTime.UtcNow - TimeSpan.FromMinutes(60)) user.AccessedTime = DateTime.UtcNow;
        await _simpleUserDbContext.SaveChangesAsync();

        // convert to simple user
        simpleUser = new SimpleUser
        {
            UserId = user.UserId.ToString(),
            AuthorizationCode = user.AuthCode,
            UserRoles = user.UserRoles!.Select(x => new SimpleUserRole(x.Role!.RoleName, x.AppId)).ToArray() //not user RoleName as Id
        };

        _memoryCache.Set(email, simpleUser, _simpleUserOptions.CacheTimeout);
        return simpleUser;
    }

    public Task<User<string>> Create(UserCreateRequest<string> request)
        => Create<string>(request);

    public async Task<User<T>> Create<T>(UserCreateRequest<T> request)
    {
        var res = await _simpleUserDbContext.Users.AddAsync(new Models.UserModel()
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedTime = DateTime.UtcNow,
            Description = request.Description,
            AuthCode = Guid.NewGuid().ToString(),
            ExData = UserConverter.ConvertExDataToString(request.ExData)
        });
        await _simpleUserDbContext.SaveChangesAsync();

        return res.Entity.ToDto<T>();
    }


    public async Task Update<T>(Guid userId, UserUpdateRequest<T> request)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        if (request.FirstName != null) userModel.FirstName = request.FirstName;
        if (request.LastName != null) userModel.LastName = request.LastName;
        if (request.Description != null) userModel.Description = request.Description;
        if (request.Email != null) userModel.Email = request.Email;
        if (request.ExData != null) userModel.ExData = UserConverter.ConvertExDataToString(request.ExData);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public Task<User<string>> Get(Guid userId)
        => Get<string>(userId);

    public async Task<User<T>> Get<T>(Guid userId)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        return userModel.ToDto<T>();
    }

    public Task<User<string>?> FindByEmail(string email)
        => FindByEmail<string>(email);

    public async Task<User<T>?> FindByEmail<T>(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleOrDefaultAsync(x => x.Email == email);
        return userModel?.ToDto<T>();
    }

    public Task<User<string>> GetByEmail(string email)
        => GetByEmail<string>(email);

    public async Task<User<T>> GetByEmail<T>(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.Email == email);
        return userModel.ToDto<T>();
    }

    public async Task Remove(Guid userId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        var userModel = new Models.UserModel { UserId = userId };
        _simpleUserDbContext.Users.Remove(userModel);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task ResetAuthorizationCode(Guid userId)
    {
        var user = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        user.AuthCode = Guid.NewGuid().ToString();
        await _simpleUserDbContext.SaveChangesAsync();
        _memoryCache.Remove(GetUserCacheKey(user.Email));
    }
}
