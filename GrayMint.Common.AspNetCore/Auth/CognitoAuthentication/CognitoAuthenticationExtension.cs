using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;

public static class CognitoAuthenticationExtension
{
    public static AuthenticationBuilder AddCognitoAuthentication(this AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
    {
        var cognitoOptions = configuration.Get<CognitoAuthenticationOptions>();
        if (cognitoOptions == null)
            throw new Exception($"Could not load {nameof(CognitoAuthenticationOptions)}.");
        cognitoOptions.Validate();

        authenticationBuilder
            .AddJwtBearer(CognitoAuthenticationDefaults.AuthenticationScheme, options =>
            {
                var cognitoArn = new AwsArn(cognitoOptions.CognitoArn);
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    ValidIssuer = cognitoOptions.CognitoArn,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                };
                options.MetadataAddress = $"https://{cognitoArn.Service}.{cognitoArn.Region}.amazonaws.com/{cognitoArn.ResourceId}/.well-known/openid-configuration";

                options.SaveToken = true;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                        var tokenValidator = scope.ServiceProvider.GetRequiredService<CognitoTokenValidator>();
                        await tokenValidator.Validate(context);
                    }
                };
            });

        authenticationBuilder.Services.AddSingleton<CognitoTokenValidator>();
        authenticationBuilder.Services.Configure<CognitoAuthenticationOptions>(configuration);
        return authenticationBuilder;
    }
}