using System.Security.Claims;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GrayMint.Common.AspNetCore;

public static class AppCommonExtension
{
    public static void RegisterAppCommonServices(this WebApplicationBuilder builder, RegisterServicesOptions options)
    {
        var services = builder.Services;

        // configure settings
        var appCommonSettings = builder.Configuration.GetSection("App").Get<AppCommonSettings>()
                                ?? throw new Exception("Could not load App section in appsettings.json file.");
        appCommonSettings.Validate();
        builder.Services.Configure<AppCommonSettings>(builder.Configuration.GetSection("App"));


        // auth settings
        var appAuthSettings = builder.Configuration.GetSection("Auth").Get<AppAuthSettings>()
                              ?? throw new Exception("Could not find Auth section in appsettings.json file.");
        appAuthSettings.Validate(builder.Environment.IsProduction());
        builder.Services.Configure<AppAuthSettings>(builder.Configuration.GetSection("Auth"));

        // cors
        if (options.AddCors)
            services.AddCors(o => o.AddPolicy(AppCommon.CorsPolicyName, corsPolicyBuilder =>
            {
                corsPolicyBuilder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetPreflightMaxAge(TimeSpan.FromHours(24 * 30));
            }));

        // Add services to the container.
        if (options.AddControllers)
            services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        if (options.AddSwagger)
        {
            services.AddEndpointsApiExplorer();
            services.AddAppSwagger(appCommonSettings.AppName);
        }

        // Add authentications
        if (options.AddBotAuthentication || options.AddCognitoAuthentication)
        {
            // bot authentication
            var authenticationBuilder = services.AddAuthentication();

            //todo add legacy 
            authenticationBuilder.AddJwtBearer(AppCommonSettings.LegacyAuthScheme, jwtBearerOptions =>
            {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RequireSignedTokens = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(appCommonSettings.AuthKey)),
                    ValidIssuer = appCommonSettings.AuthIssuer,
                    ValidAudience = appCommonSettings.AuthIssuer,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(TokenValidationParameters.DefaultClockSkew.TotalSeconds)
                };
                jwtBearerOptions.Events = new JwtBearerEvents()
                {
                    OnTokenValidated = async context =>
                    {
                        await Task.Delay(0);
                        var claimsIdentity = new ClaimsIdentity();
                        foreach (var claim in context.Principal!.Claims.Where(x => x.Type == ClaimTypes.Role))
                            claimsIdentity.AddClaim(new Claim("app-role", $"{claim.Value}/apps/*"));
                    }
                };
            });


            if (options.AddBotAuthentication)
                authenticationBuilder.AddBotAuthentication(builder.Configuration.GetSection("Auth"));

            // Cognito authentication
            if (options.AddCognitoAuthentication)
                authenticationBuilder.AddCognitoAuthentication(builder.Configuration.GetSection("Auth"));
        }

        if (options.AddMemoryCache)
            builder.Services.AddMemoryCache();

        if (options.AddHttpClient)
            builder.Services.AddHttpClient();
    }

    public static void UseAppCommonServices(this WebApplication webApplication, UseServicesOptions options)
    {
        if (options.UseCors)
            webApplication.UseCors(AppCommon.CorsPolicyName);

        // Configure the HTTP request pipeline.
        webApplication.UseHttpsRedirection();

        if (options.UseSwagger)
        {
            webApplication.UseOpenApi();
            webApplication.UseSwaggerUi3();
        }

        if (options.UseAuthentication)
            webApplication.UseAuthentication();

        if (options.UseAuthorization)
            webApplication.UseAuthorization();

        if (options.UseAppExceptions)
            webApplication.UseAppExceptionHandler();

        if (options.MapControllers)
            webApplication.MapControllers();
    }

}