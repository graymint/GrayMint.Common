using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using GrayMint.Common.AspNetCore.Auth.SimpleAuthorization;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore;

public static class AppCommon
{
    public const string CorsPolicyName = "AllowAllCorsPolicy";
    internal const string OptionsValidationMsgTemplate = "The App in AppSettings must be initialized properly. OptionName: {0}";

    public static void RegisterServices(WebApplicationBuilder builder, RegisterServicesOptions options) 
    {
        var services = builder.Services;

        // configure settings
        var appCommonSettings = builder.Configuration.GetSection("App").Get<AppCommonSettings>()
                                ?? throw new Exception("Could not load App section in appsettings.json file.");
        appCommonSettings.Validate();


        // auth settings
        var appAuthSettings = builder.Configuration.GetSection("Auth").Get<AppAuthSettings>()
                              ?? throw new Exception("Could not find Auth section in appsettings.json file.");
        appAuthSettings.Validate(builder.Environment.IsProduction());

        // cors
        if (options.AddCors)
            services.AddCors(o => o.AddPolicy(CorsPolicyName, corsPolicyBuilder =>
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
            if (options.AddBotAuthentication)
                authenticationBuilder.AddBotAuthentication(builder.Configuration.GetSection("Auth"));

            // Cognito authentication
            if (options.AddCognitoAuthentication)
                authenticationBuilder.AddCognitoAuthentication(builder.Configuration.GetSection("Auth"));
        }

        // Add authorization policies
        if (options.AddSimpleAuthorization)
            services.AddSimpleAuthorization(builder.Configuration.GetSection("Auth"));

        if (options.AddMemoryCache)
            builder.Services.AddMemoryCache();

        if (options.AddHttpClient)
            builder.Services.AddHttpClient();
    }

    public static void UseServices(WebApplication webApplication, UseServicesOptions options)
    {
        if (options.UseCors)
            webApplication.UseCors(CorsPolicyName);

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

    public static async Task CheckDatabaseCommand<T>(WebApplication webApplication, string[] args) where T : DbContext
    {
        var logger = webApplication.Services.GetRequiredService<ILogger<WebApplication>>();

        await using var scope = webApplication.Services.CreateAsyncScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<T>();
        if (args.Contains("/recreateDb", StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation($"Recreating the {nameof(T)} database...");
            await appDbContext.Database.EnsureDeletedAsync();
        }
        await appDbContext.Database.EnsureCreatedAsync();
    }

    public static async Task RunAsync(WebApplication webApplication, string[] args)
    {
        var logger = webApplication.Services.GetRequiredService<ILogger<WebApplication>>();
        if (args.Contains("/initOnly", StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("Initialize mode prevents application to start.");
            return;
        }

        await webApplication.RunAsync();
    }
}