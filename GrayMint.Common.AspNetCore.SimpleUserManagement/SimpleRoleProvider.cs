using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConveters;
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

    public async Task AddUser(string roleId, string userId, string appId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        await _simpleUserDbContext.UserRoles.AddAsync(
           new Models.UserRole
           {
               RoleId = int.Parse(roleId),
               UserId = int.Parse(userId),
               AppId = appId,
           });
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task RemoveUser(string roleId, string userId, string appId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();
        _simpleUserDbContext.UserRoles.Remove(
            new Models.UserRole
            {
                UserId = int.Parse(userId),
                RoleId = int.Parse(roleId),
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
        var roleModel = await _simpleUserDbContext.Roles.AddAsync(new Models.Role()
        {
            RoleName = request.RoleName,
            Description = request.Description
        });
        await _simpleUserDbContext.SaveChangesAsync();

        return roleModel.Entity.ToDto();
    }

    public async Task<Role[]> GetAll()
    {
        var roleModels = await _simpleUserDbContext.Roles.ToArrayAsync();
        return roleModels.Select(x => x.ToDto()).ToArray();
    }

    public async Task<Role> Get(string roleId)
    {
        var userModel = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == int.Parse(roleId));
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

    public async Task Remove(string roleId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        var roleModel = new Models.Role { RoleId = int.Parse(roleId) };
        _simpleUserDbContext.Roles.Remove(roleModel);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task<UserRole[]> GetUserRoles(string roleId)
    {
        var roleModels = await _simpleUserDbContext.UserRoles
            .Include(x => x.Role)
            .Where(x => x.RoleId == int.Parse(roleId))
            .ToArrayAsync();

        return roleModels.Select(x => x.ToDto()).ToArray();
    }

    public async Task<UserRole[]> GetUserRolesByUser(string userId)
    {
        var roleModels = await _simpleUserDbContext.UserRoles
            .Include(x => x.Role)
            .Where(x => x.UserId == int.Parse(userId))
            .ToArrayAsync();

        return roleModels.Select(x => x.ToDto()).ToArray();
    }

    public async Task Update(string roleId, RoleUpdateRequest request)
    {
        var roleModel = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == int.Parse(roleId));
        if (request.RoleName != null) roleModel.RoleName = request.RoleName;
        if (request.Description != null) roleModel.Description = request.Description;
        await _simpleUserDbContext.SaveChangesAsync();
    }
}


