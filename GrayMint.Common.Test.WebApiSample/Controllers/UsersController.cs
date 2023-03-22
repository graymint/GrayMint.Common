using System.Net.Mime;
using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
[Route("/api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly SimpleUserProvider _simpleUserProvider;
    private readonly BotAuthenticationTokenBuilder _botAuthenticationTokenBuilder;

    public UsersController(
        SimpleUserProvider simpleUserProvider, 
        BotAuthenticationTokenBuilder botAuthenticationTokenBuilder)
    {
        _simpleUserProvider = simpleUserProvider;
        _botAuthenticationTokenBuilder = botAuthenticationTokenBuilder;
    }

    [AuthorizePermission(Permission.SystemRead)]
    [HttpGet("{email}/authorization-token")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<string> GetAuthorizationTokenByEmail(string email, bool createNew)
    {
        if (createNew)
        {
            var user = await _simpleUserProvider.GetByEmail(email) ?? throw new KeyNotFoundException($"Could not find any user by email. email: {email}");
            await _simpleUserProvider.ResetAuthorizationCode(user.UserId);
        }
        var token = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(Guid.NewGuid().ToString(), email);
        return token.Parameter!;
    }

    [Authorize(SimpleRoleAuth.Policy)]
    [HttpPost("reset-authorization-token")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<string> ResetMyToken()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ??
                    throw new UnauthorizedAccessException("You don't have email claim.");

        var user = await _simpleUserProvider.GetByEmail(email) 
                   ?? throw new KeyNotFoundException($"Could not find any user by email. email: {email}");

        await _simpleUserProvider.ResetAuthorizationCode(user.UserId);
        var token = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(email, email);
        return token.Parameter!;
    }
}