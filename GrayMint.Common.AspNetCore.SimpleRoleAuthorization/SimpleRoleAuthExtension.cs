using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuthExtension
{
    public static IServiceCollection AddSimpleRoleAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SimpleRoleAuth.Policy, new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(BotAuthenticationDefaults.AuthenticationScheme)
                .AddAuthenticationSchemes(CognitoAuthenticationDefaults.AuthenticationScheme)
                .AddAuthenticationSchemes(AppCommonSettings.LegacyAuthScheme)
                .AddRequirements(new SimpleRoleAuthRequirement())
                .RequireAuthenticatedUser()
                .Build());
        });

        services.AddScoped<IBotAuthenticationProvider, SimpleUserResolver>();
        services.AddScoped<IAuthorizationHandler, SimpleRoleAuthHandler>();
        services.AddTransient<IClaimsTransformation, SimpleRoleAuthClaimsTransformation>();
        services.AddScoped<SimpleUserResolver>();
        return services;
    }
}