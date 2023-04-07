using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Dtos;
using GrayMint.Common.AspNetCore.SimpleUserControllers.Exceptions;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.AspNetCore.SimpleUserManagement.Dtos;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers.Services;

public class UserService
{
    private readonly SimpleRoleProvider _simpleRoleProvider;
    private readonly SimpleUserProvider _simpleUserProvider;
    private readonly BotAuthenticationTokenBuilder _botAuthenticationTokenBuilder;
    private readonly SimpleUserControllerOptions _userControllerOptions;

    public UserService(
        SimpleRoleProvider simpleRoleProvider,
        SimpleUserProvider simpleUserProvider, 
        BotAuthenticationTokenBuilder botAuthenticationTokenBuilder, 
        IOptions<SimpleUserControllerOptions> userControllerOptions)
    {
        _simpleRoleProvider = simpleRoleProvider;
        _simpleUserProvider = simpleUserProvider;
        _botAuthenticationTokenBuilder = botAuthenticationTokenBuilder;
        _userControllerOptions = userControllerOptions.Value;
    }

    public async Task<Guid> GetUserId(ClaimsPrincipal claimsPrincipal)
    {
        var simpleUser = await _simpleUserProvider.FindSimpleUser(claimsPrincipal);
        if (simpleUser == null)
            throw new UnregisteredUser("User has not been registered.");

        return Guid.Parse(simpleUser.UserId);
    }

    public Task<User> Get(Guid userId)
    {
        return _simpleUserProvider.Get(userId);
    }

    public async Task<UserRole> GetUserRole(string appId, Guid userId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(userId: userId, appId: appId);
        return userRoles.Single();
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

    public async Task<IEnumerable<UserRole>> GetAppUserRoles(Guid userId)
    {
        var userRoles = await _simpleRoleProvider.GetUserRoles(userId: userId);
        return userRoles.Where(x => x.AppId != "*");
    }

    public async Task<ApiKeyResult> ResetApiKey(Guid userId)
    {
        if (!_userControllerOptions.AllowUserApiKey)
            throw new InvalidOperationException($"{nameof(_userControllerOptions.AllowUserApiKey)} is not enabled.");

        var user = await Get(userId);
        await _simpleUserProvider.ResetAuthorizationCode(user.UserId);
        var authenticationHeader = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(user.UserId.ToString(), user.Email);
        var ret = new ApiKeyResult
        {
            UserId = userId,
            Authorization = authenticationHeader.ToString(),
        };

        return ret;
    }
}