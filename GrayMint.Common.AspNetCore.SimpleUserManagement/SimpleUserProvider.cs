using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserProvider : ISimpleAuthUserProvider
{
    private readonly SimpleUserDbContext _simpleUserDbContext;

    public SimpleUserProvider(SimpleUserDbContext simpleUserDbContext)
    {
        _simpleUserDbContext = simpleUserDbContext;
    }

    public async Task<SimpleAuthUser?> GetAuthUser(string email)
    {
        var userModel = await _simpleUserDbContext.Users
            .Include(x=>x.UserRoles)!
            .ThenInclude(x=>x.Role)
            .SingleAsync(x => x.Email == email);

        var authUser = new SimpleAuthUser
        {
            AuthCode = userModel.AuthCode,
            UserRoles = userModel.UserRoles!.Select(x => new SimpleAuthUserRole(x.Role!.RoleName,x.AppId)).ToArray()
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
            AuthCode = "1",
        });
        await _simpleUserDbContext.SaveChangesAsync();

        var ret = UserFromModel(res.Entity);
        return ret;
    }

    public async Task<User> Get(string userId)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.UserId == int.Parse(userId));
        var ret = UserFromModel(userModel);
        return ret;
    }

    public async Task<User> GetByEmail(string email)
    {
        var userModel = await _simpleUserDbContext.Users.SingleAsync(x => x.Email == email);
        var ret = UserFromModel(userModel);
        return ret;
    }

    internal static User UserFromModel(Models.User userModel)
    {
        var user = new User(userModel.UserId.ToString(), email: userModel.Email, createdTime: userModel.CreatedTime)
        {
            AuthCode = userModel.AuthCode,
            FirstName = userModel.FirstName,
            LastName = userModel.LastName,
            Description = userModel.Description,
        };

        return user;
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

    public static void RegisterSimpleUserProvider(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? optionsAction = null)
    {
        builder.Services.AddDbContext<SimpleUserDbContext>(optionsAction);
        builder.Services.AddScoped<ISimpleAuthUserProvider, SimpleUserProvider>();
        builder.Services.AddScoped<SimpleUserProvider>();
        builder.Services.AddScoped<SimpleRoleProvider>();
    }

    public static async Task UseSimpleUserProvider(WebApplication webApplication)
    {
        await using var scope = webApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SimpleUserDbContext>();
        await dbContext.EnsureTablesCreated();

        await dbContext.SaveChangesAsync();
    }
}