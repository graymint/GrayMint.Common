using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Security;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Services;

public class TeamService
{
    private readonly SimpleRoleProvider _simpleRoleProvider;
    private readonly SimpleUserProvider _simpleUserProvider;
    private readonly BotAuthenticationTokenBuilder _botAuthenticationTokenBuilder;
    private readonly SimpleRoleAuthService _simpleRoleAuthService;
    private readonly SimpleRoleAuthOptions _simpleRoleAuthOptions;
    private readonly SimpleUserControllerOptions _userControllerOptions;

    public TeamService(
        SimpleRoleProvider simpleRoleProvider,
        SimpleUserProvider simpleUserProvider,
        BotAuthenticationTokenBuilder botAuthenticationTokenBuilder,
        SimpleRoleAuthService simpleRoleAuthService,
        IOptions<SimpleRoleAuthOptions> simpleRoleAuthOptions,
        IOptions<SimpleUserControllerOptions> simpleUserControllerOptions)
    {
        _simpleRoleProvider = simpleRoleProvider;
        _simpleUserProvider = simpleUserProvider;
        _botAuthenticationTokenBuilder = botAuthenticationTokenBuilder;
        _simpleRoleAuthService = simpleRoleAuthService;
        _userControllerOptions = simpleUserControllerOptions.Value;
        _simpleRoleAuthOptions = simpleRoleAuthOptions.Value;
    }

    public Task<IEnumerable<SimpleRole>> GetSimpleRoles(string appId)
    {
        var isSystem = appId == "*";
        var simpleRoles = _simpleRoleAuthOptions.Roles ?? Array.Empty<SimpleRole>();
        var appSimpleRoles = simpleRoles.Where(x => x.IsSystem == isSystem);
        return Task.FromResult(appSimpleRoles);
    }

    public async Task<bool> IsAppOwnerRole(string appId, Guid roleId)
    {
        if (appId == "*") return false; //system app does not have app owner

        var appRoles = await GetSimpleRoles(appId);
        var simpleRole = appRoles.SingleOrDefault(x => x.RoleId == roleId);
        return simpleRole != null && simpleRole.Permissions.Contains(TeamPermissions.AppTeamWriteOwner);
    }

    public async Task<bool> CheckUserPermission(ClaimsPrincipal claimsPrincipal, string appId, string permission)
    {
        var ret = await _simpleRoleAuthService.AuthorizePermissionAsync(claimsPrincipal, appId, TeamPermissions.AppTeamWriteOwner);
        return ret.Succeeded;
    }

    public async Task ValidateWriteAccessToRole(ClaimsPrincipal claimsPrincipal, string appId, Guid roleId)
    {
        // check is role belong to AppRoles
        var appRoles = await GetSimpleRoles(appId);
        var simpleRole = appRoles.SingleOrDefault(x => x.RoleId == roleId);
        if (simpleRole == null)
            throw new NotExistsException("The role is not an app role.");

        // check is role the owner
        if (!await IsAppOwnerRole(appId, roleId))
            return;

        //App level permission
        if (!await CheckUserPermission(claimsPrincipal, appId, TeamPermissions.AppTeamWriteOwner))
            throw new UnauthorizedAccessException();
    }

    public async Task<ApiKeyResult> CreateBot(string appId, TeamAddBotParam addParam)
    {
        if (!_userControllerOptions.AllowBotAppOwner && await IsAppOwnerRole(appId, addParam.RoleId))
            throw new InvalidOperationException("Bot can not be an owner.");

        // check is a bot already exists with the same name
        var userRoles = await ListUsers(appId);
        if (userRoles.Any(x => addParam.Name.Equals(x.User.FirstName, StringComparison.OrdinalIgnoreCase) && x.User.IsBot))
            throw new AlreadyExistsException("Bots");

        var email = $"{Guid.NewGuid()}@bot";
        var user = await _simpleUserProvider.Create(new UserCreateRequest
        {
            Email = email,
            FirstName = addParam.Name,
            IsBot = true
        });

        await _simpleRoleProvider.AddUser(roleId: addParam.RoleId, userId: user.UserId, appId: appId);
        var authenticationHeader = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(user.UserId.ToString(), user.Email);
        var ret = new ApiKeyResult
        {
            UserId = user.UserId,
            Authorization = authenticationHeader.ToString()
        };
        return ret;
    }

    public async Task<ApiKeyResult> ResetBotApiKey(string appId, Guid userId)
    {
        var userRole = await GetUserRole(appId, userId);

        // Security Concern! an app owner should not be able to reset user ApiKey because they may used by other apps
        if (!userRole.User.IsBot)
            throw new InvalidOperationException("Only a bot ApiKey can be reset by this api.");

        await _simpleUserProvider.ResetAuthorizationCode(userRole.User.UserId);
        var authenticationHeader = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(userRole.User.UserId.ToString(), userRole.User.Email);
        var ret = new ApiKeyResult
        {
            UserId = userId,
            Authorization = authenticationHeader.ToString(),
        };
        return ret;
    }

    public async Task<UserRole> AddUser(string appId, TeamAddUserParam addParam)
    {
        // create user if not found
        var user = await _simpleUserProvider.FindByEmail(addParam.Email);
        user ??= await _simpleUserProvider.Create(new UserCreateRequest { Email = addParam.Email });
        if (user.IsBot)
            throw new InvalidOperationException("Bot can not be added. You need to create a new one or update it.");

        var userRoles = await _simpleRoleProvider.GetUserRoles(appId: appId, userId: user.UserId);
        if (userRoles.Any())
            throw new AlreadyExistsException("Users");

        await _simpleRoleProvider.AddUser(roleId: addParam.RoleId, userId: user.UserId, appId: appId);
        userRoles = await _simpleRoleProvider.GetUserRoles(userId: user.UserId, appId: appId);
        return userRoles.Single(x => x.User.UserId == user.UserId);
    }

    public async Task<UserRole> GetUserRole(string appId, Guid userId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(userId: userId, appId: appId);
        return userRoles.Single(x => x.User.UserId == userId);
    }

    public async Task<UserRole> UpdateUser(string appId, Guid userId, TeamUpdateUserParam updateParam)
    {
        var userRole = await GetUserRole(appId, userId);

        if (updateParam.RoleId != null)
        {
            //remove the old role
            await _simpleRoleProvider.RemoveUser(userRole.Role.RoleId, userId, appId);

            // add to new role
            await _simpleRoleProvider.AddUser(updateParam.RoleId, userId, appId);
        }

        userRole = await GetUserRole(appId, userId);
        return userRole;
    }

    public async Task RemoveUser(string appId, Guid userId)
    {
        var userRole = await GetUserRole(appId, userId);

        // remove the user
        await _simpleRoleProvider.RemoveUser(userRole.Role.RoleId, userId, appId);

        // remove user from repo if he is not in any role
        var allUserRoles = await _simpleRoleProvider.GetUserRoles(userId);
        if (!allUserRoles.Any())
            await _simpleUserProvider.Remove(userId);
    }

    public async Task<IEnumerable<UserRole>> ListUsers(string appId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(appId: appId);
        return userRoles;
    }

    public async Task<IEnumerable<Role>> ListRoles(string appId)
    {
        var simpleRoles = await GetSimpleRoles(appId);

        var roles = simpleRoles.Select(x => new Role
        {
            RoleId = x.RoleId,
            RoleName = x.RoleName,
            Description = ""
        });

        return roles;
    }

    public async Task<ApiKeyResult> CreateSystemAdminApiKey()
    {
        if (!_userControllerOptions.IsTestEnvironment)
            throw new UnauthorizedAccessException();

        var systemRoles = await GetSimpleRoles("*");
        var systemAdminRole = systemRoles.FirstOrDefault(x => x.Permissions.Contains(TeamPermissions.SystemTeamWrite));
        if (systemAdminRole == null)
            throw new NotExistsException($"Could not find {nameof(TeamPermissions.SystemTeamWrite)} in any system roles.");

        var user = await CreateBot("*", new TeamAddBotParam { Name = $"TestAdmin_{Guid.NewGuid()}", RoleId = systemAdminRole.RoleId });
        return user;
    }
}