using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Security;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Controllers;

[Authorize]
[Route("/api/v{version:apiVersion}/team")]
public abstract class TeamControllerBase<TResource, TResourceId, TUser, TUserRole, TRole>
    : ControllerBase where TResourceId : notnull
{
    protected readonly RoleService RoleService;

    protected abstract TUser ToDto(User user);
    protected abstract TRole ToDto(Role role);
    protected abstract TUserRole ToDto(UserRole user);
    protected abstract string ToResourceId(TResourceId appId);
    protected abstract Task<IEnumerable<TResource>> GetResources(string[] resourceId);

    protected TeamControllerBase(
        RoleService roleService)
    {
        RoleService = roleService;
    }

    [HttpPost("users/system/api-key")]
    [AllowAnonymous]
    public async Task<ApiKeyResult> CreateSystemApiKey()
    {
        if (!RoleService.Options.IsTestEnvironment)
            throw new UnauthorizedAccessException();

        var res = await RoleService.CreateSystemAdminApiKey();
        return res;
    }

    [HttpPost("users/current/register")]
    [Authorize]
    public virtual async Task<TUser> RegisterCurrentUser()
    {
        if (!RoleService.Options.AllowUserSelfRegister)
            throw new UnauthorizedAccessException("Self-Register is not enabled.");

        var res = await RoleService.Register(User);
        return ToDto(res);
    }

    [HttpGet("users/current")]
    [Authorize]
    public async Task<TUser> GetCurrentUser()
    {
        var userId = await RoleService.GetUserId(User);
        var ret = await RoleService.GetUser(userId);
        return ToDto(ret);
    }

    [HttpPost("users/current/reset-api-key")]
    [Authorize]
    public async Task<ApiKeyResult> ResetCurrentUserApiKey()
    {
        var userId = await RoleService.GetUserId(User);
        var res = await RoleService.ResetUserApiKey(userId);
        return res;
    }

    [Authorize]
    [HttpGet("users/current/resources")]
    public async Task<IEnumerable<TResource>> ListCurrentUserResources()
    {
        var userId = await RoleService.GetUserId(User);
        var userRoles = await RoleService.GetUserRoles(userId: userId);
        var resourceIds = userRoles.Distinct().Select(x => x.AppId);
        return await GetResources(resourceIds.ToArray());
    }

    [HttpGet("resources/{resourceId}/roles")]
    public async Task<IEnumerable<TRole>> ListRoles(TResourceId resourceId)
    {
        await VerifyRoleReadPermission(User, resourceId);
        var roles = await RoleService.GetRoles(ToResourceId(resourceId));
        return roles.Select(ToDto);
    }

    [HttpGet("resources/{resourceId}/users")]
    public async Task<IEnumerable<TUserRole>> ListUsers(TResourceId resourceId)
    {
        await VerifyRoleReadPermission(User, resourceId);
        var res = await RoleService.GetUsers(ToResourceId(resourceId));
        return res.Select(ToDto);
    }

    [HttpPost("resources/{resourceId}/bots")]
    public async Task<ApiKeyResult> CreateBot(TResourceId resourceId, TeamAddBotParam addParam)
    {
        await VerifyWritePermission(User, resourceId, addParam.RoleId);

        if (!RoleService.Options.AllowBotAppOwner && await RoleService.IsResourceOwnerRole(ToResourceId(resourceId), addParam.RoleId))
            throw new InvalidOperationException("Bot can not be an owner.");

        var res = await RoleService.CreateBot(ToResourceId(resourceId), addParam);
        return res;
    }

    [HttpPost("resources/{resourceId}/bots/{userId:guid}/reset-api-key")]
    public async Task<ApiKeyResult> ResetBotApiKey(TResourceId resourceId, Guid userId)
    {
        var userRoles = await VerifyWritePermission(resourceId, userId);

        // check is a bot user
        if (!userRoles.First().User.IsBot)
            throw new InvalidOperationException("Only a bot ApiKey can be reset by this api.");

        var res = await RoleService.ResetUserApiKey(userId);
        return res;
    }

    [HttpPost("resources/{resourceId}/users")]
    public async Task<TUserRole> AddUser(TResourceId resourceId, TeamAddUserParam addParam)
    {
        await VerifyWritePermission(User, resourceId, addParam.RoleId);

        var user = await RoleService.FindUserByEmail(addParam.Email);
        if (user?.IsBot == true)
            throw new InvalidOperationException("Bot can not be added. You need to create a new one or update it.");

        var res = await RoleService.AddUserByEmail(ToResourceId(resourceId), addParam.RoleId, addParam.Email);
        return ToDto(res);
    }

    [HttpGet("resources/{resourceId}/users/{userId:guid}")]
    public async Task<TUserRole> GetUser(TResourceId resourceId, Guid userId)
    {
        await VerifyRoleReadPermission(User, resourceId);

        var res = await RoleService.GetUserRoles(ToResourceId(resourceId), userId);
        return ToDto(res.First());
    }

    [HttpPost("resources/{resourceId}/users/{userId:guid}")]
    public async Task<TUserRole> UpdateUser(TResourceId resourceId, Guid userId, TeamUpdateUserParam updateParam)
    {
        var userRoles = await VerifyWritePermission(resourceId, userId);

        if (updateParam.RoleId != null)
        {
            await VerifyWritePermission(User, resourceId, updateParam.RoleId);
            await VerifyAppOwnerPolicy(resourceId, userId, updateParam.RoleId);

            // remove from other roles
            foreach (var userRole in userRoles.Where(x => x.Role.RoleId != updateParam.RoleId))
                await RoleService.RemoveUser(ToResourceId(resourceId), userRole.Role.RoleId, userId);

            // add to role
            if (userRoles.All(x => x.Role.RoleId != updateParam.RoleId))
                await RoleService.AddUser(ToResourceId(resourceId), updateParam.RoleId, userId);

            // delete if user does not have any more roles in the system
            if (!(await RoleService.GetUserRoles(userId)).Any())
                await RoleService.DeleteUser(userId);
        }

        var res = await RoleService.GetUserRoles(ToResourceId(resourceId), userId);
        return ToDto(res.Single()); //throw error if it is more than one
    }

    [HttpDelete("resources/{resourceId}/users/{userId:guid}")]
    public async Task RemoveUser(TResourceId resourceId, Guid userId)
    {
        var userRoles = await VerifyWritePermission(resourceId, userId);

        // Check owner policy
        await VerifyAppOwnerPolicy(resourceId, userId, null);

        // remove from all roles
        foreach (var userRole in userRoles)
            await RoleService.RemoveUser(ToResourceId(resourceId), userRole.Role.RoleId, userRole.User.UserId);

        // delete if user does not have any more roles in the system
        if (!(await RoleService.GetUserRoles(userId)).Any())
            await RoleService.DeleteUser(userId);
    }

    private async Task VerifyRoleReadPermission(ClaimsPrincipal claimsPrincipal, TResourceId resourceId)
    {
        if (!await RoleService.CheckUserPermission(claimsPrincipal, ToResourceId(resourceId), RolePermissions.RoleRead))
            throw new UnauthorizedAccessException();
    }

    private async Task<UserRole[]> VerifyWritePermission(TResourceId resourceId, Guid userId)
    {
        // check user permission over all of the user roles
        var userRoles = await RoleService.GetUserRoles(ToResourceId(resourceId), userId);
        if (!userRoles.Any())
            throw new UnauthorizedAccessException();

        foreach (var userRole in userRoles)
            await VerifyWritePermission(User, resourceId, userRole.Role.RoleId);

        return userRoles;
    }

    private async Task VerifyWritePermission(ClaimsPrincipal claimsPrincipal, TResourceId resourceId, Guid roleId)
    {
        if (!await RoleService.CheckUserPermission(claimsPrincipal, ToResourceId(resourceId), RolePermissions.RoleWrite))
            throw new UnauthorizedAccessException();

        //Check AppTeamWriteOwner
        if (await RoleService.IsResourceOwnerRole(ToResourceId(resourceId), roleId) &&
            !await RoleService.CheckUserPermission(claimsPrincipal, ToResourceId(resourceId), RolePermissions.RoleWriteOwner))
            throw new UnauthorizedAccessException();
    }


    // can not change its own owner role unless it has global TeamWrite permission
    private async Task VerifyAppOwnerPolicy(TResourceId resourceId, Guid userId, Guid? newRoleId)
    {
        // check is AllowOwnerSelfRemove allowed
        if (RoleService.Options.AllowOwnerSelfRemove)
            return;

        // check is caller changing himself
        var callerUserId = await RoleService.GetUserId(User);
        if (callerUserId != userId)
            return;

        // check is caller the owner of the resource
        var callerUserRoles = await RoleService.GetUserRoles(ToResourceId(resourceId), callerUserId);
        var isCallerOwner = false;
        foreach (var callerUserRole in callerUserRoles)
            isCallerOwner |= await RoleService.IsResourceOwnerRole(ToResourceId(resourceId), callerUserRole.Role.RoleId);
        if (!isCallerOwner)
            return;

        // error if the new role is not owner
        var isNewRoleOwner = newRoleId != null && await RoleService.IsResourceOwnerRole(ToResourceId(resourceId), newRoleId.Value);
        if (!isNewRoleOwner)
            throw new InvalidOperationException("You are an owner and can not remove yourself. Ask other owners or delete the project.");
    }
}

public abstract class TeamControllerBase<TResource, TResourceId>
    : TeamControllerBase<TResource, TResourceId, User, UserRole, Role> where TResourceId : notnull
{
    protected TeamControllerBase(RoleService roleService) : base(roleService)
    {
    }

    protected override User ToDto(User user) => user;
    protected override Role ToDto(Role role) => role;
    protected override UserRole ToDto(UserRole userRole) => userRole;
}
