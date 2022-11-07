using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConveters;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserProvider : ISimpleRoleAuthUserProvider
{
    private readonly SimpleUserDbContext _simpleUserDbContext;

    public SimpleUserProvider(SimpleUserDbContext simpleUserDbContext)
    {
        _simpleUserDbContext = simpleUserDbContext;
    }

    public async Task ResetAuthCodeByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.Email == email);
        userModel.AuthCode = Guid.NewGuid().ToString();
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task<SimpleUser?> GetAuthUserByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users
            .Include(x => x.UserRoles)!
            .ThenInclude(x => x.Role)
            .SingleAsync(x => x.Email == email);

        var authUser = new SimpleUser
        {
            AuthCode = userModel.AuthCode,
            UserRoles = userModel.UserRoles!.Select(x => new SimpleUserRole(x.Role!.RoleName, x.AppId)).ToArray()
        };
        return authUser;
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

    public async Task<User> Get(string userId)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == int.Parse(userId));
        return userModel.ToDto();
    }

    public async Task<User?> GetByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleOrDefaultAsync(x => x.Email == email);
        return userModel?.ToDto();
    }

    public async Task Update(string userId, UserUpdateRequest request)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == int.Parse(userId));
        if (request.FirstName != null) userModel.FirstName = request.FirstName;
        if (request.LastName != null) userModel.LastName = request.LastName;
        if (request.Description != null) userModel.Description = request.Description;
        if (request.Email != null) userModel.Email = request.Email;
        await _simpleUserDbContext.SaveChangesAsync();
    }
    public async Task Remove(string userId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        var userModel = new Models.User { UserId = int.Parse(userId) };
        _simpleUserDbContext.Users.Remove(userModel);
        await _simpleUserDbContext.SaveChangesAsync();
    }
}
