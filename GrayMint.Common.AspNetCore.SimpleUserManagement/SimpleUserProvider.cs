using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserProvider : ISimpleUserProvider
{
    private readonly SimpleUserDbContext _simpleUserDbContext;
    private readonly IMemoryCache _memoryCache;

    public SimpleUserProvider(
        SimpleUserDbContext simpleUserDbContext,
        IMemoryCache memoryCache)
    {
        _simpleUserDbContext = simpleUserDbContext;
        _memoryCache = memoryCache;
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
            .SingleAsync(x => x.Email == email);

        // update info by claims
        var givenName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName);
        var surnameName = claimsPrincipal.FindFirstValue(ClaimTypes.Surname);
        if (givenName != null) user.FirstName = givenName;
        if (surnameName != null) user.LastName = surnameName;
        await _simpleUserDbContext.SaveChangesAsync();

        // convert to simple user
        simpleUser = new SimpleUser
        {
            UserId = user.UserId.ToString(),
            AuthorizationCode = user.AuthCode,
            UserRoles = user.UserRoles!.Select(x => new SimpleUserRole(x.Role!.RoleName, x.AppId)).ToArray() //not user RoleName as Id
        };

        _memoryCache.Set(email, simpleUser);
        return simpleUser;
    }

    public async Task<User> Create(UserCreateRequest request)
    {
        var res = await _simpleUserDbContext.Users.AddAsync(new Models.UserModel()
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedTime = DateTime.UtcNow,
            Description = request.Description,
            AuthCode = Guid.NewGuid().ToString()
        });
        await _simpleUserDbContext.SaveChangesAsync();

        return res.Entity.ToDto();
    }

    public async Task<User> Get(Guid userId)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        return userModel.ToDto();
    }

    public async Task<User?> FindByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleOrDefaultAsync(x => x.Email == email);
        return userModel?.ToDto();
    }

    public async Task<User> GetByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.Email == email);
        return userModel.ToDto();
    }

    public async Task Update(Guid userId, UserUpdateRequest request)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        if (request.FirstName != null) userModel.FirstName = request.FirstName;
        if (request.LastName != null) userModel.LastName = request.LastName;
        if (request.Description != null) userModel.Description = request.Description;
        if (request.Email != null) userModel.Email = request.Email;
        await _simpleUserDbContext.SaveChangesAsync();
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
