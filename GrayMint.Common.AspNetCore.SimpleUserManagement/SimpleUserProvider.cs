using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserProvider : ISimpleUserProvider
{
    private readonly SimpleUserDbContext _simpleUserDbContext;
    private readonly SimpleRoleAuthCache _simpleRoleAuthCache;

    public SimpleUserProvider(
        SimpleUserDbContext simpleUserDbContext, 
        SimpleRoleAuthCache simpleRoleAuthCache)
    {
        _simpleUserDbContext = simpleUserDbContext;
        _simpleRoleAuthCache = simpleRoleAuthCache;
    }

    public async Task<SimpleUser?> FindSimpleUserByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users
            .Include(x => x.UserRoles)!
            .ThenInclude(x => x.Role)
            .SingleAsync(x => x.Email == email);

        var simpleUser = new SimpleUser
        {
            UserId = userModel.UserId.ToString(),
            AuthorizationCode = userModel.AuthCode,
            UserRoles = userModel.UserRoles!.Select(x => new SimpleUserRole(x.Role!.RoleName, x.AppId)).ToArray() //not user RoleName as Id
        };
        return simpleUser;
    }

    public async Task<User> Create(UserCreateRequest request)
    {
        var res = await _simpleUserDbContext.Users.AddAsync(new Models.User()
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

        var userModel = new Models.User { UserId = userId };
        _simpleUserDbContext.Users.Remove(userModel);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task ResetAuthorizationCode(Guid userId)
    {
        var user = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == userId);
        user.AuthCode = Guid.NewGuid().ToString();
        await _simpleUserDbContext.SaveChangesAsync();
        await _simpleRoleAuthCache.ClearUserCache(user.Email);
    }
}
