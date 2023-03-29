using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.Test.WebApiSample.Persistence;
using GrayMint.Common.Test.WebApiSample.Security;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var authConfiguration = builder.Configuration.GetSection("Auth");

        builder.AddGrayMintCommonServices(new GrayMintCommonOptions { AppName = "Web App Sample" }, new RegisterServicesOptions());
        builder.Services
            .AddAuthentication()
            .AddBotAuthentication(authConfiguration.Get<BotAuthenticationOptions>(), builder.Environment.IsProduction())
            .AddCognitoAuthentication(authConfiguration.Get<CognitoAuthenticationOptions>());

        builder.Services.AddGrayMintSimpleRoleAuthorization(new SimpleRoleAuthOptions { Roles = Roles.All });
        builder.Services.AddGrayMintSimpleUserProvider(authConfiguration.Get<SimpleUserOptions>(), options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));
        builder.Services.AddDbContext<WebApiSampleDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));

        // Add services to the container.
        var webApp = builder.Build();
        webApp.UseGrayMintCommonServices(new UseServicesOptions());
        await GrayMintApp.CheckDatabaseCommand<WebApiSampleDbContext>(webApp.Services, args);
        await webApp.UseGrayMintSimpleUserProvider();

        await GrayMintApp.RunAsync(webApp, args);

    }
}