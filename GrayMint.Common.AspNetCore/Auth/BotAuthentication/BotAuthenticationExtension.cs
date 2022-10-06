using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public static class BotAuthenticationExtension
{
    public static AuthenticationBuilder AddBotAuthentication(this AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
    {
        var botAuthenticationOptions = configuration.Get<BotAuthenticationOptions>();
        var securityKey = new SymmetricSecurityKey(botAuthenticationOptions.BotKey);

        authenticationBuilder
            .AddJwtBearer(BotAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RequireSignedTokens = true,
                    IssuerSigningKey = securityKey,
                    ValidIssuer = botAuthenticationOptions.BotIssuer,
                    ValidAudience = botAuthenticationOptions.BotIssuer,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(TokenValidationParameters.DefaultClockSkew.TotalSeconds)
                };
                options.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = async context =>
                    {
                        await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                        var tokenValidator = scope.ServiceProvider.GetRequiredService<BotTokenValidator>();
                        await tokenValidator.Validate(context);
                    }
                };
            });

        authenticationBuilder.Services.Configure<BotAuthenticationOptions>(configuration);
        authenticationBuilder.Services.AddScoped<BotTokenValidator>();
        authenticationBuilder.Services.AddScoped<BotAuthenticationTokenBuilder>();
        return authenticationBuilder;
    }

    private class BotTokenValidator
    {
        private readonly IBotAuthenticationProvider _botAuthenticationProvider;

        public BotTokenValidator(IBotAuthenticationProvider botAuthenticationProvider)
        {
            _botAuthenticationProvider = botAuthenticationProvider;
        }

        public async Task Validate(TokenValidatedContext context)
        {
            if (context.Principal == null)
            {
                context.Fail("Principal has not been validated.");
                return;
            }

            var authCode = await _botAuthenticationProvider.GetAuthCode(context.Principal);
            if (string.IsNullOrEmpty(authCode))
            {
                context.Fail($"{BotAuthenticationDefaults.AuthenticationScheme} needs authCode.");
                return;
            }

            // deserialize access token
            var tokenAuthCode = context.Principal.Claims.SingleOrDefault(x => x.Type == "AuthCode")?.Value;
            if (string.IsNullOrEmpty(authCode))
            {
                context.Fail("Could not find AuthCode in the token.");
                return;
            }

            if (authCode != tokenAuthCode)
            {
                context.Fail("Invalid AuthCode.");
            }
        }
    }
}