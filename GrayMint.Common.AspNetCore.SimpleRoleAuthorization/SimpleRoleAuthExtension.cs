using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public static class SimpleRoleAuthExtension
{
    public static IServiceCollection AddGrayMintSimpleRoleAuthorization(this IServiceCollection services,
        SimpleRoleAuthOptions roleAuthOptions)
    {
        services.AddAuthorization(options =>
        {
            // add permission policies
            if (roleAuthOptions.RolePermissions != null)
                foreach (var permissionId in roleAuthOptions.RolePermissions.SelectMany(x => x.PermissionIds).Distinct())
                    options.AddPolicy(SimpleRoleAuth.CreatePolicyNameForPermission(permissionId),
                        CreatePolicy(services, roleAuthOptions)
                            .AddRequirements(new SimplePermissionAuthRequirement(permissionId))
                            .Build());

            // add RolePolicy
            var rolePolicy = CreatePolicy(services, roleAuthOptions).Build();
            options.AddPolicy(SimpleRoleAuth.Policy, rolePolicy);
            options.DefaultPolicy = rolePolicy;
        });

        services.AddSingleton(Options.Create(roleAuthOptions));
        services.AddSingleton<SimpleRoleAuthCache>();
        services.AddScoped<IBotAuthenticationProvider, SimpleUserResolver>();
        services.AddScoped<IAuthorizationHandler, SimpleRoleAuthHandler>();
        services.AddScoped<IAuthorizationHandler, SimplePermissionAuthHandler>();
        services.AddTransient<IClaimsTransformation, SimpleRoleAuthClaimsTransformation>();
        services.AddScoped<SimpleUserResolver>();
        return services;
    }

    private static AuthorizationPolicyBuilder CreatePolicy(IServiceCollection services, SimpleRoleAuthOptions roleAuthOptions)
    {
        var addBotAuthenticationSchemes = services.Any(x => x.ServiceType == typeof(BotTokenValidator));
        var addCognitoAuthenticationSchemes = services.Any(x => x.ServiceType == typeof(CognitoTokenValidator));

        var policyBuilder = new AuthorizationPolicyBuilder();
        if (addBotAuthenticationSchemes)
            policyBuilder.AddAuthenticationSchemes(BotAuthenticationDefaults.AuthenticationScheme);

        if (addCognitoAuthenticationSchemes) 
            policyBuilder.AddAuthenticationSchemes(CognitoAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrEmpty(roleAuthOptions.CustomAuthenticationScheme)) 
            policyBuilder.AddAuthenticationSchemes(roleAuthOptions.CustomAuthenticationScheme);

        policyBuilder.RequireAuthenticatedUser();
        return policyBuilder;
    }

}
