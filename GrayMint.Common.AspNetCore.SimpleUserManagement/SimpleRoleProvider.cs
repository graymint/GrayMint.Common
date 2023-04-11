﻿using GrayMint.Common.AspNetCore.SimpleUserManagement.DtoConverters;
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

    public async Task<UserRole> AddUser(string resourceId, Guid roleId, Guid userId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        await _simpleUserDbContext.UserRoles
            .AddAsync(new Models.UserRoleModel
            {
                RoleId = roleId,
                UserId = userId,
                ResourceId = resourceId,
            });
        await _simpleUserDbContext.SaveChangesAsync();

        var userRoles = await ListUserRoles(resourceId: resourceId, roleId: roleId, userId);
        return userRoles.Items.Single();
    }

    public async Task RemoveUser(string resourceId, Guid roleId, Guid userId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();
        _simpleUserDbContext.UserRoles.Remove(
            new Models.UserRoleModel
            {
                UserId = userId,
                RoleId = roleId,
                ResourceId = resourceId
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
        var role = await _simpleUserDbContext.Roles.AddAsync(new Models.RoleModel()
        {
            RoleId = request.RoleId ?? Guid.NewGuid(),
            RoleName = request.RoleName,
            Description = request.Description
        });
        await _simpleUserDbContext.SaveChangesAsync();

        return role.Entity.ToDto();
    }

    public async Task<Role[]> List()
    {
        var roles = await _simpleUserDbContext.Roles.ToArrayAsync();
        return roles.Select(x => x.ToDto()).ToArray();
    }

    public async Task<Role> Get(Guid roleId)
    {
        var user = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == roleId);
        return user.ToDto();
    }

    public async Task<Role?> FindByName(string roleName)
    {
        var role = await _simpleUserDbContext.Roles.SingleOrDefaultAsync(x => x.RoleName == roleName);
        return role?.ToDto();
    }


    public async Task<Role> GetByName(string roleName)
    {
        var role = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleName == roleName);
        return role.ToDto();
    }

    public async Task Remove(Guid roleId)
    {
        _simpleUserDbContext.ChangeTracker.Clear();

        var role = new Models.RoleModel { RoleId = roleId };
        _simpleUserDbContext.Roles.Remove(role);
        await _simpleUserDbContext.SaveChangesAsync();
    }

    public async Task<ListResult<UserRole>> ListUserRoles(
        string? resourceId = null, Guid? roleId = null, Guid? userId = null,
        string? search = null, bool? isBot = null,
        int recordIndex = 0, int? recordCount = null)
    {
        recordCount ??= int.MaxValue;
        if (!Guid.TryParse(search, out var searchGuid)) searchGuid = Guid.Empty;

        await using var trans = await _simpleUserDbContext.WithNoLockTransaction();
        var query = _simpleUserDbContext.UserRoles
            .Include(x => x.Role)
            .Include(x => x.User)
            .Where(x =>
                (isBot == null || x.User!.IsBot == isBot) &&
                (roleId == null || x.RoleId == roleId) &&
                (userId == null || x.UserId == userId) &&
                (resourceId == null || x.ResourceId == resourceId))
            .Where(x =>
                string.IsNullOrEmpty(search) ||
                (x.UserId == searchGuid && searchGuid != Guid.Empty) ||
                (x.User!.FirstName != null && x.User.FirstName.StartsWith(search)) ||
                (x.User!.LastName != null && x.User.LastName.StartsWith(search)) ||
                (x.User.Email.StartsWith(search)));

        var results = await query
            .OrderBy(x => x.ResourceId)
            .ThenBy(x => x.User!.Email)
            .Skip(recordIndex)
            .Take(recordCount ?? int.MaxValue)
            .ToArrayAsync();

        var ret = new ListResult<UserRole>
        {
            TotalCount = results.Length < recordCount ? recordIndex + results.Length : await query.LongCountAsync(),
            Items = results.Select(x => x.ToDto()).ToArray()
        };

        return ret;
    }

    public async Task Update(Guid roleId, RoleUpdateRequest request)
    {
        var role = await _simpleUserDbContext.Roles.SingleAsync(x => x.RoleId == roleId);
        if (request.RoleName != null) role.RoleName = request.RoleName;
        if (request.Description != null) role.Description = request.Description;
        await _simpleUserDbContext.SaveChangesAsync();
    }
}