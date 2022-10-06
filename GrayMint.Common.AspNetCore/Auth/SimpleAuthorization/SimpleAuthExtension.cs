using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;

public static class SimpleAuthExtension
{
    public static IServiceCollection AddSimpleAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SimpleAuth.Policy, new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(BotAuthenticationDefaults.AuthenticationScheme)
                .AddAuthenticationSchemes(CognitoAuthenticationDefaults.AuthenticationScheme)
                .AddAuthenticationSchemes(AppCommonSettings.LegacyAuthScheme)
                .AddRequirements(new SimpleAuthRequirement())
                .RequireAuthenticatedUser()
                .Build());
        });
        
        services.AddScoped<IBotAuthenticationProvider, SimpleAuthUserResolver>();
        services.AddScoped<IAuthorizationHandler, SimpleAuthHandler>();
        services.AddTransient<IClaimsTransformation, SimpleAuthClaimsTransformation>();
        services.AddScoped<SimpleAuthUserResolver>();
        return services;
    }
}