using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using GrayMint.Common.AspNetCore.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotAuthenticationTokenBuilder
{
    private readonly IBotAuthenticationProvider _botAuthenticationProvider;
    private readonly BotAuthenticationOptions _botAuthenticationOptions;

    public BotAuthenticationTokenBuilder(IBotAuthenticationProvider botAuthenticationProvider, IOptions<BotAuthenticationOptions> botAuthenticationOptions)
    {
        _botAuthenticationProvider = botAuthenticationProvider;
        _botAuthenticationOptions = botAuthenticationOptions.Value;
    }

    public Task<AuthenticationHeaderValue> CreateAuthenticationHeader(string subject, string email)
    {
        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, subject));
        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, email));
        return CreateAuthenticationHeader(claimsIdentity);
    }

    public async Task<AuthenticationHeaderValue> CreateAuthenticationHeader(ClaimsIdentity claimsIdentity)
    {
        // get authcode by standard claim
        var nameClaim = claimsIdentity.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
        if (nameClaim != null) claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameClaim.Value));

        var emailClaim = claimsIdentity.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Email);
        if (emailClaim != null) claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, emailClaim.Value));

        var authCode = await _botAuthenticationProvider.GetAuthorizationCode(new ClaimsPrincipal(claimsIdentity));
        claimsIdentity.AddClaim(new Claim(BotAuthenticationDefaults.AuthorizationCodeTypeName, authCode));

        if (nameClaim != null) claimsIdentity.RemoveClaim(claimsIdentity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier));
        if (emailClaim != null) claimsIdentity.RemoveClaim(claimsIdentity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Email));

        // create jwt
        var audience = string.IsNullOrEmpty(_botAuthenticationOptions.BotAudience) ? _botAuthenticationOptions.BotIssuer : _botAuthenticationOptions.BotAudience;
        var jwt = JwtUtil.CreateSymmetricJwt(
            _botAuthenticationOptions.BotKey,
            _botAuthenticationOptions.BotIssuer,
            audience,
            null, //read from claims,
            null,
            claimsIdentity.Claims.ToArray());

        return new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
    }

}