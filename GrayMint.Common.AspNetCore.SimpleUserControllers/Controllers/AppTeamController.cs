using System.Security.Claims;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Security;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Controllers;

public abstract class AppTeamController<TAppId, TApp, TUser, TUserRole, TRole>
    : SystemTeamController<TUser, TUserRole, TRole> where TAppId : notnull
{
    protected abstract TAppId ToAppIdDto(string appId);
    protected abstract Task<IEnumerable<TApp>> GetApps(IEnumerable<TAppId> appIds);

    protected AppTeamController(TeamService teamService, UserService userService) : base(teamService, userService)
    {
    }

    [HttpGet("apps/{appId}/roles")]
    [AuthorizePermission(TeamPermissions.AppTeamRead)]
    public async Task<IEnumerable<TRole>> ListAppRoles(TAppId appId)
    {
        var roles = await TeamService.ListRoles(ToAppId(appId));
        return roles.Select(ToDto);
    }

    [HttpGet("apps/{appId}/users")]
    [AuthorizePermission(TeamPermissions.AppTeamRead)]
    public async Task<IEnumerable<TUserRole>> ListAppUsers(TAppId appId)
    {
        var res = await TeamService.ListUsers(ToAppId(appId));
        return res.Select(ToDto);
    }

    [HttpGet("apps/{appId}/bots")]
    [AuthorizePermission(TeamPermissions.AppTeamWrite)]
    public async Task<ApiKeyResult> CreateAppBot(TAppId appId, TeamAddBotParam addParam)
    {
        await TeamService.ValidateWriteAccessToRole(User, ToAppId(appId), addParam.RoleId);
        var res = await TeamService.CreateBot(ToAppId(appId), addParam);
        return res;
    }

    [HttpPost("apps/{appId}/bots/{userId:guid}/reset-api-key")]
    [AuthorizePermission(TeamPermissions.AppTeamWrite)]
    public async Task<ApiKeyResult> ResetAppBotApiKey(TAppId appId, Guid userId)
    {
        var res = await TeamService.ResetBotApiKey(ToAppId(appId), userId);
        return res;
    }

    [HttpPost("apps/{appId}/users")]
    [AuthorizePermission(TeamPermissions.AppTeamWrite)]
    public async Task<TUserRole> AddAppUser(TAppId appId, TeamAddUserParam addParam)
    {
        await TeamService.ValidateWriteAccessToRole(User, ToAppId(appId), addParam.RoleId);
        var res = await TeamService.AddUser(ToAppId(appId), addParam);
        return ToDto(res);
    }

    [HttpGet("apps/{appId}/users/{userId:guid}")]
    [AuthorizePermission(TeamPermissions.AppTeamRead)]
    public async Task<TUserRole> GetAppUser(TAppId appId, Guid userId)
    {
        var res = await TeamService.GetUserRole(ToAppId(appId), userId);
        return ToDto(res);

    }

    [HttpPost("apps/{appId}/users/{userId:guid}")]
    [AuthorizePermission(TeamPermissions.AppTeamWrite)]
    public async Task<TUserRole> UpdateAppUser(TAppId appId, Guid userId, TeamUpdateUserParam updateParam)
    {
        var userRole = await TeamService.GetUserRole(ToAppId(appId), userId);
        if (updateParam.RoleId != null)
        {
            await TeamService.ValidateWriteAccessToRole(User, ToAppId(appId), updateParam.RoleId);
            await TeamService.ValidateWriteAccessToRole(User, ToAppId(appId), userRole.Role.RoleId);
            await ValidateAppOwnerPolicy(User, ToAppId(appId), userId, updateParam.RoleId);
        }

        var res = await TeamService.UpdateUser(ToAppId(appId), userId, updateParam);
        return ToDto(res);
    }

    [HttpDelete("apps/{appId}/users/userId")]
    [AuthorizePermission(TeamPermissions.AppTeamWrite)]
    public async Task RemoveAppUser(TAppId appId, Guid userId)
    {
        var userRole = await TeamService.GetUserRole(ToAppId(appId), userId);
        await TeamService.ValidateWriteAccessToRole(User, ToAppId(appId), userRole.Role.RoleId);
        await ValidateAppOwnerPolicy(User, ToAppId(appId), userId, userRole.Role.RoleId);
        await TeamService.RemoveUser(ToAppId(appId), userId);
    }

    [Authorize]
    [HttpGet("users/current/apps")]
    public async Task<IEnumerable<TApp>> GetCurrentUserApps()
    {
        var userId = await UserService.GetUserId(User);
        var userRoles = await UserService.GetAppUserRoles(userId: userId);
        var appIds = userRoles.Select(x => ToAppIdDto(x.AppId));
        return await GetApps(appIds);
    }

    protected string ToAppId(TAppId appId)
    {
        var appIdString = appId.ToString();

        if (string.IsNullOrEmpty(appIdString) || appIdString == "*")
            throw new ArgumentException("Invalid appId", nameof(appId));

        return appIdString;
    }

    // can not change its own owner role unless it has global TeamWrite permission
    private async Task ValidateAppOwnerPolicy(ClaimsPrincipal claimsPrincipal, string appId, Guid userId, Guid roleId)
    {
        if (userId == await UserService.GetUserId(claimsPrincipal) && await TeamService.IsAppOwnerRole(appId, roleId) &&
            !await TeamService.CheckUserPermission(claimsPrincipal, "*", TeamPermissions.AppTeamWrite))
            throw new InvalidOperationException("You are an owner and can not remove yourself. Ask other owners or delete the project.");
    }
}

public abstract class AppTeamController<TAppId, TApp> 
    : AppTeamController<TAppId, TApp, User, UserRole, Role> where TAppId : notnull
{
    protected AppTeamController(TeamService teamService, UserService userService) : base(teamService, userService)
    {
    }

    protected override User ToDto(User user) => user;
    protected override Role ToDto(Role role) => role;
    protected override UserRole ToDto(UserRole userRole) => userRole;
}
