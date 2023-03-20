using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuthExtension
{
    public static IServiceCollection AddGrayMintSimpleRoleAuthorization(this IServiceCollection services, 
        IConfiguration configuration, 
        bool requireBotAuthentication, 
        bool requireCognitoAuthentication,
        string? customAuthenticationScheme = null)
    {
        var simpleRoleAuthOptions = configuration.Get<SimpleRoleAuthOptions>();
        simpleRoleAuthOptions?.Validate();

        services.AddAuthorization(options =>
        {
            var policyBuilder = new AuthorizationPolicyBuilder();
            if (requireBotAuthentication) policyBuilder.AddAuthenticationSchemes(BotAuthenticationDefaults.AuthenticationScheme);
            if (requireCognitoAuthentication) policyBuilder.AddAuthenticationSchemes(CognitoAuthenticationDefaults.AuthenticationScheme);
            if (!string.IsNullOrEmpty(customAuthenticationScheme)) policyBuilder.AddAuthenticationSchemes(customAuthenticationScheme);
            var policy = policyBuilder
                .AddRequirements(new SimpleRoleAuthRequirement())
                .RequireAuthenticatedUser()
                .Build();
            
            options.AddPolicy(SimpleRoleAuth.Policy, policy);
            options.DefaultPolicy = policy;
        });

        services.Configure<SimpleRoleAuthOptions>(configuration);
        services.AddScoped<IBotAuthenticationProvider, SimpleUserResolver>();
        services.AddScoped<IAuthorizationHandler, SimpleRoleAuthHandler>();
        services.AddTransient<IClaimsTransformation, SimpleRoleAuthClaimsTransformation>();
        services.AddScoped<SimpleUserResolver>();
        return services;
    }
}