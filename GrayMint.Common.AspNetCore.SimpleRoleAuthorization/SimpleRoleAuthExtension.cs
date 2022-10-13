using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuthExtension
{
    public static IServiceCollection AddSimpleRoleAuthorization(this IServiceCollection services, bool requireBotAuthentication, bool requireCognitoAuthentication)
    {
        services.AddAuthorization(options =>
        {
            var policyBuilder = new AuthorizationPolicyBuilder();
            if (requireBotAuthentication) policyBuilder.AddAuthenticationSchemes(BotAuthenticationDefaults.AuthenticationScheme);
            if (requireCognitoAuthentication) policyBuilder.AddAuthenticationSchemes(CognitoAuthenticationDefaults.AuthenticationScheme);
            options.AddPolicy(SimpleRoleAuth.Policy,
                policyBuilder
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