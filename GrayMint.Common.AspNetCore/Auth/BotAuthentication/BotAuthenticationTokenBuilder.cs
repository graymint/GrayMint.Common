using System.Net.Http.Headers;
using System.Security.Claims;
using GrayMint.Common.AspNetCore.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotAuthenticationTokenBuilder
{
    private readonly IBotAuthenticationProvider _botAuthenticationProvider;
    private readonly IOptions<BotAuthenticationOptions> _botAuthenticationOptions;

    public BotAuthenticationTokenBuilder(IBotAuthenticationProvider botAuthenticationProvider, IOptions<BotAuthenticationOptions> botAuthenticationOptions)
    {
        _botAuthenticationProvider = botAuthenticationProvider;
        _botAuthenticationOptions = botAuthenticationOptions;
    }

    public async Task<AuthenticationHeaderValue> CreateAuthenticationHeader(string email)
    {
        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, email));
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, email));
        var principal = new ClaimsPrincipal(claimsIdentity);
        
        var authCode = await _botAuthenticationProvider.GetAuthCode(principal);

        return new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme,
            JwtUtil.CreateAccessToken(_botAuthenticationOptions.Value.BotKey, _botAuthenticationOptions.Value.BotIssuer, authCode, email, email, null));
    }
}