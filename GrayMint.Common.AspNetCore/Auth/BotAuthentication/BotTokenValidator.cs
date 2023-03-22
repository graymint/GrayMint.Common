using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotTokenValidator
{
    private readonly IBotAuthenticationProvider _botAuthenticationProvider;

    public BotTokenValidator(
        IBotAuthenticationProvider botAuthenticationProvider)
    {
        _botAuthenticationProvider = botAuthenticationProvider;
    }

    public async Task Validate(TokenValidatedContext context)
    {
        try
        {
            if (context.Principal == null)
                throw new Exception("Principal has not been validated.");

            var authCode = await _botAuthenticationProvider.GetAuthorizationCode(context.Principal);
            if (string.IsNullOrEmpty(authCode))
                throw new Exception($"{BotAuthenticationDefaults.AuthenticationScheme} needs {BotAuthenticationDefaults.AuthorizationCodeTypeName}.");

            // deserialize access token
            var tokenAuthCode = context.Principal.Claims.SingleOrDefault(x => x.Type == BotAuthenticationDefaults.AuthorizationCodeTypeName)?.Value;
            if (string.IsNullOrEmpty(tokenAuthCode))
                throw new Exception($"Could not find {BotAuthenticationDefaults.AuthorizationCodeTypeName} in the token.");

            if (authCode != tokenAuthCode)
                throw new Exception($"Invalid {BotAuthenticationDefaults.AuthorizationCodeTypeName}.");

        }
        catch (Exception ex)
        {
            context.Fail(ex.Message);
        }
    }
}