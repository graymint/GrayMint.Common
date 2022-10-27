using System.Net.Mime;
using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrayMint.Common.Test.WebApiSample.Controllers;

[ApiController]
[Route("/api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly SimpleUserProvider _simpleUserProvider;
    private readonly BotAuthenticationTokenBuilder _botAuthenticationTokenBuilder;

    public UsersController(SimpleUserProvider simpleUserProvider, BotAuthenticationTokenBuilder botAuthenticationTokenBuilder)
    {
        _simpleUserProvider = simpleUserProvider;
        _botAuthenticationTokenBuilder = botAuthenticationTokenBuilder;
    }

    [Authorize(SimpleRoleAuth.Policy, Roles = Roles.AppCreator)]
    [HttpGet("{email}/auth-token")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<string> GetAuthTokenByEmail(string email, bool createNew)
    {
        if (createNew)
            await _simpleUserProvider.ResetAuthCodeByEmail(email);
        var token = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(Guid.NewGuid().ToString(), email);
        return token.Parameter!;
    }

    [Authorize(SimpleRoleAuth.Policy)]
    [HttpPost("{appId}/reset-auth-token")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<string> ResetMyToken()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ??
                    throw new UnauthorizedAccessException("You don't have email claim.");

        await _simpleUserProvider.ResetAuthCodeByEmail(email);
        var token = await _botAuthenticationTokenBuilder.CreateAuthenticationHeader(email, email);
        return token.Parameter!;
    }
}