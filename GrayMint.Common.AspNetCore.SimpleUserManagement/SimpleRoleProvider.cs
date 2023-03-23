using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;
using GrayMint.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleRoleProvider
{
    private readonly SimpleUserDbContext _simpleUserDbContext;

    public SimpleRoleProvider(SimpleUserDbContext simpleUserDbContext)
    {
        _simpleUserDbContext = simpleUserDbContext;
    }

    public async Task AddUser(Guid roleId, Guid userId, string appId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        await _simpleUserDbContext.UserRoles.AddAsync(
           new Models.UserRoleModel
           {
               RoleId = roleId,
               UserId = userId,
               AppId = appId,
           });
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task RemoveUser(Guid roleId, Guid userId, string appId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();
        _simpleUserDbContext.UserRoles.Remove(
            new Models.UserRoleModel
            {
                UserId = userId,
                RoleId = roleId,
                AppId = appId
            });

        try
        {
            await _simpleUserDbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("The database operation was expected to affect 1 row(s), but actually affected 0 row(s)"))
        {
            throw new NotExistsException();
        }
    }

    public async Task<Role> Create(RoleCreateRequest request)
    {
        _simpleUserDbContext.ChangeTracker.Clear();
        var roleModel = await _simpleUserDbContext.Roles.AddAsync(new Models.RoleModel()
        {
            RoleName = request.RoleName,
            Description = request.Description
        });
        await _simpleUserDbContext.SaveChangesAsync();

        return roleModel.Entity.ToDto();
    }

    public async Task<Role[]> List()
    {
        var roleModels = await _simpleUserDbContext.Roles.ToArrayAsync();
        return roleModels.Select(x => x.ToDto()).ToArray();
    }

    public async Task<Role> Get(Guid roleId)
    {
        var userModel = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == roleId);
        return userModel.ToDto();
    }

    public async Task<Role?> FindByName(string roleName)
    {
        var roleModel = await _simpleUserDbContext.Roles.SingleOrDefaultAsync(x => x.RoleName == roleName);
        return roleModel?.ToDto();
    }


    public async Task<Role> GetByName(string roleName)
    {
        var roleModel = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleName == roleName);
        return roleModel.ToDto();
    }

    public async Task Remove(Guid roleId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        var roleModel = new Models.RoleModel { RoleId = roleId };
        _simpleUserDbContext.Roles.Remove(roleModel);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task<UserRole[]> GetUserRoles(Guid? roleId = null, Guid? userId = null, string? appId = null)
    {
        var roleModels = await _simpleUserDbContext.UserRoles
            .Include(x => x.Role)
            .Include(x => x.User)
            .Where(x =>
                (roleId == null || x.RoleId == roleId) &&
                (userId == null || x.UserId == userId) &&
                (appId == null || x.AppId == appId))
            .ToArrayAsync();

        return roleModels.Select(x => x.ToDto()).ToArray();
    }

    public async Task Update(Guid roleId, RoleUpdateRequest request)
    {
        var roleModel = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == roleId);
        if (request.RoleName != null) roleModel.RoleName = request.RoleName;
        if (request.Description != null) roleModel.Description = request.Description;
        await _simpleUserDbContext.SaveChangesAsync();
    }
}


