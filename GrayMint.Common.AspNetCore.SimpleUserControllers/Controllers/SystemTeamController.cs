using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Security;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Controllers;

[Route("/api/v{version:apiVersion}/team")]
public abstract class SystemTeamController<TUser, TUserRole, TRole>
    : ControllerBase
{
    protected readonly TeamService TeamService;
    protected readonly UserService UserService;
    protected abstract TUser ToDto(User user);
    protected abstract TRole ToDto(Role role);
    protected abstract TUserRole ToDto(UserRole user);

    protected SystemTeamController(
        TeamService teamService,
        UserService userService)
    {
        TeamService = teamService;
        UserService = userService;
    }

    [HttpPost("users/current/register")]
    [Authorize]
    public virtual async Task<TUser> RegisterCurrentUser()
    {
        var res = await UserService.Register(User);
        return ToDto(res);
    }

    [HttpGet("users/current")]
    [Authorize]
    public async Task<TUser> GetCurrentUser()
    {
        var userId = await UserService.GetUserId(User);
        var ret = await UserService.Get(userId);
        return ToDto(ret);
    }

    [HttpPost("users/current/reset-api-key")]
    [Authorize]
    public async Task<ApiKeyResult> ResetCurrentUserApiKey()
    {
        var userId = await UserService.GetUserId(User);
        var res = await UserService.ResetApiKey(userId);
        return res;
    }

    [HttpGet("system/roles")]
    [AuthorizePermission(TeamPermissions.SystemTeamRead)]
    public async Task<IEnumerable<TRole>> ListSystemRoles()
    {
        var roles = await TeamService.ListRoles("*");
        return roles.Select(ToDto);
    }

    [HttpGet("system/users")]
    [AuthorizePermission(TeamPermissions.SystemTeamRead)]
    public async Task<IEnumerable<TUserRole>> ListSystemUsers()
    {
        var res = await TeamService.ListUsers("*");
        return res.Select(ToDto);
    }

    [HttpPost("system/users/system-admin-api-key")]
    [AllowAnonymous]
    public async Task<ApiKeyResult> CreateSystemApiKey()
    {
        var res = await TeamService.CreateSystemAdminApiKey();
        return res;
    }

    [HttpGet("system/bots")]
    [AuthorizePermission(TeamPermissions.SystemTeamWrite)]
    public async Task<ApiKeyResult> CreateSystemBot(TeamAddBotParam addParam)
    {
        var res = await TeamService.CreateBot("*", addParam);
        return res;
    }

    [HttpPost("system/bots/{userId:guid}/reset-api-key")]
    [AuthorizePermission(TeamPermissions.SystemTeamWrite)]
    public async Task<ApiKeyResult> ResetSystemBotApiKey(Guid userId)
    {
        var res = await TeamService.ResetBotApiKey("*", userId);
        return res;
    }

    [HttpPost("system/users")]
    [AuthorizePermission(TeamPermissions.SystemTeamWrite)]
    public async Task<TUserRole> AddSystemUser(TeamAddUserParam addParam)
    {
        var res = await TeamService.AddUser("*", addParam);
        return ToDto(res);
    }

    [HttpGet("system/users/{userId:guid}")]
    [AuthorizePermission(TeamPermissions.SystemTeamRead)]
    public async Task<TUserRole> GetSystemUser(Guid userId)
    {
        var res = await TeamService.GetUserRole("*", userId);
        return ToDto(res);
    }

    [HttpPost("system/users/{userId:guid}")]
    [AuthorizePermission(TeamPermissions.SystemTeamWrite)]
    public async Task<TUserRole> UpdateSystemUser(Guid userId, TeamUpdateUserParam updateParam)
    {
        var res = await TeamService.UpdateUser("*", userId, updateParam);
        return ToDto(res);
    }

    [HttpDelete("system/users/{userId:guid}")]
    [AuthorizePermission(TeamPermissions.SystemTeamWrite)]
    public async Task RemoveSystemUser(Guid userId)
    {
        await TeamService.RemoveUser("*", userId);
    }
}

public class SystemTeamController : SystemTeamController<User, UserRole, Role> 
{
    protected SystemTeamController(TeamService teamService, UserService userService) 
        : base(teamService, userService)
    {
    }

    protected override User ToDto(User user) => user;
    protected override Role ToDto(Role role) => role;
    protected override UserRole ToDto(UserRole userRole) => userRole;
}
