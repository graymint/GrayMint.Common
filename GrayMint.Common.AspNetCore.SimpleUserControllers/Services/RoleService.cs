using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Exceptions;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Security;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using GrayMint.Common.Exceptions;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Services;

public class RoleService
{
    private readonly SimpleRoleProvider _simpleRoleProvider;
    private readonly SimpleUserProvider _simpleUserProvider;
    private readonly BotAuthenticationTokenBuilder _botAuthenticationTokenBuilder;
    private readonly SimpleRoleAuthService _simpleRoleAuthService;
    private readonly SimpleRoleAuthOptions _simpleRoleAuthOptions;
    public SimpleUserControllerOptions Options { get; }

    public RoleService(
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
        Options = simpleUserControllerOptions.Value;
        _simpleRoleAuthOptions = simpleRoleAuthOptions.Value;
    }

    public async Task<Guid> GetUserId(ClaimsPrincipal claimsPrincipal)
    {
        var simpleUser = await _simpleUserProvider.FindSimpleUser(claimsPrincipal);
        if (simpleUser == null)
            throw new UnregisteredUser();

        return Guid.Parse(simpleUser.UserId);
    }

    private Task<IEnumerable<SimpleRole>> GetSimpleRoles(string resourceId)
    {
        var isSystem = resourceId == "*";
        var simpleRoles = _simpleRoleAuthOptions.Roles ?? Array.Empty<SimpleRole>();
        var appSimpleRoles = simpleRoles.Where(x => x.IsSystem == isSystem);
        return Task.FromResult(appSimpleRoles);
    }

    public async Task<string[]> GetRolePermissions(string resourceId, Guid roleId)
    {
        var appRoles = await GetSimpleRoles(resourceId);
        var simpleRole = appRoles.Single(x => x.RoleId == roleId);
        return simpleRole.Permissions;
    }

    public async Task<bool> IsResourceOwnerRole(string resourceId, Guid roleId)
    {
        if (resourceId == "*") return false; //system app does not have app owner

        var permissions = await GetRolePermissions(resourceId, roleId);
        return permissions.Contains(RolePermissions.RoleWriteOwner);
    }

    public async Task<bool> CheckUserPermission(ClaimsPrincipal claimsPrincipal, string resourceId, string permission)
    {
        var ret = await _simpleRoleAuthService.AuthorizePermissionAsync(claimsPrincipal, resourceId, permission);
        return ret.Succeeded;
    }

    public async Task<ApiKeyResult> CreateBot(string resourceId, TeamAddBotParam addParam)
    {
        // check is a bot already exists with the same name
        var userRoles = await GetUsers(resourceId);
        if (userRoles.Any(x => addParam.Name.Equals(x.User.FirstName, StringComparison.OrdinalIgnoreCase) && x.User.IsBot))
            throw new AlreadyExistsException("Bots");

        var email = $"{Guid.NewGuid()}@bot";
        var user = await _simpleUserProvider.Create(new UserCreateRequest
        {
            Email = email,
            FirstName = addParam.Name,
            IsBot = true
        });

        await _simpleRoleProvider.AddUser(roleId: addParam.RoleId, userId: user.UserId, appId: resourceId);
        var authenticationHeader = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(user.UserId.ToString(), user.Email);
        var ret = new ApiKeyResult
        {
            UserId = user.UserId,
            Authorization = authenticationHeader.ToString()
        };
        return ret;
    }

    public async Task<ApiKeyResult> ResetUserApiKey(Guid userId)
    {
        var user = await _simpleUserProvider.Get(userId);
        await _simpleUserProvider.ResetAuthorizationCode(user.UserId);
        var authenticationHeader = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(user.UserId.ToString(), user.Email);
        var ret = new ApiKeyResult
        {
            UserId = userId,
            Authorization = authenticationHeader.ToString(),
        };

        return ret;
    }

    public async Task<UserRole> AddUserByEmail(string resourceId, Guid roleId, string email)
    {
        // create user if not found
        var user = await _simpleUserProvider.FindByEmail(email);
        user ??= await _simpleUserProvider.Create(new UserCreateRequest { Email = email });
        return await AddUser(resourceId, roleId, user.UserId);
    }

    public async Task<UserRole> AddUser(string resourceId, Guid roleId, Guid userId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(appId: resourceId, userId: userId);
        if (userRoles.Any())
            throw new AlreadyExistsException("Users");

        await _simpleRoleProvider.AddUser(roleId: roleId, userId: userId, appId: resourceId);
        userRoles = await _simpleRoleProvider.GetUserRoles(userId: userId, appId: resourceId);
        return userRoles.Single(x => x.User.UserId == userId);
    }

    public Task<UserRole[]> GetUserRoles(string resourceId, Guid userId)
    {
        return _simpleRoleProvider.GetUserRoles(appId: resourceId, userId: userId);
    }

    public Task<UserRole[]> GetUserRoles(Guid userId)
    {
        return _simpleRoleProvider.GetUserRoles(userId: userId);
    }

    public async Task<User?> FindUserByEmail(string email)
    {
        return await _simpleUserProvider.FindByEmail(email);
    }

    public async Task<User> GetUser(Guid userId)
    {
        return await _simpleUserProvider.Get(userId);
    }

    public async Task<IEnumerable<UserRole>> GetUsers(string resourceId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(appId: resourceId);
        return userRoles;
    }
    public Task RemoveUser(string resourceId, Guid roleId, Guid userId)
    {
        return _simpleRoleProvider.RemoveUser(roleId, userId, resourceId);
    }

    public Task DeleteUser(Guid userId)
    {
        return _simpleUserProvider.Remove(userId);
    }

    public async Task<IEnumerable<Role>> GetRoles(string resourceId)
    {
        var simpleRoles = await GetSimpleRoles(resourceId);
        await _simpleRoleProvider.List();

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
        var systemRoles = await GetSimpleRoles("*");
        var systemAdminRole = systemRoles.FirstOrDefault(x => x.Permissions.Contains(RolePermissions.RoleWrite));
        if (systemAdminRole == null)
            throw new NotExistsException($"Could not find {nameof(RolePermissions.RoleWrite)} in any system roles.");

        var user = await CreateBot("*", new TeamAddBotParam { Name = $"TestAdmin_{Guid.NewGuid()}", RoleId = systemAdminRole.RoleId });
        return user;
    }

    public async Task<User> Register(ClaimsPrincipal userPrincipal)
    {
        var email =
            userPrincipal.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value.ToLower()
            ?? throw new UnauthorizedAccessException("Could not find user's email claim!");

        var ret = await _simpleUserProvider.Create(new UserCreateRequest
        {
            Email = email,
            FirstName = userPrincipal.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value,
            LastName = userPrincipal.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)?.Value,
            Description = null
        });

        return ret;
    }
}