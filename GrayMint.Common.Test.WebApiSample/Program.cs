using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.Auth.BotAuthentication;
using GrayMint.Common.AspNetCore.Auth.CognitoAuthentication;
using GrayMint.Common.AspNetCore.SimpleRoleAuthorization;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.Test.WebApiSample.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddGrayMintCommonServices(builder.Configuration.GetSection("App"), new RegisterServicesOptions());
        builder.Services
            .AddAuthentication()
            .AddBotAuthentication(builder.Configuration.GetSection("Auth"), builder.Environment.IsProduction())
            .AddCognitoAuthentication(builder.Configuration.GetSection("Auth"));

        builder.Services.AddGrayMintSimpleRoleAuthorization(builder.Configuration.GetSection("Auth"), true, true);
        builder.Services.AddGrayMintSimpleUserProvider(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));
        builder.Services.AddDbContext<WebApiSampleDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));

        // Add services to the container.
        var webApp = builder.Build();
        webApp.UseGrayMintCommonServices(new UseServicesOptions());
        await GrayMintApp.CheckDatabaseCommand<WebApiSampleDbContext>(webApp, args);
        await webApp.UseGrayMintSimpleUserProvider();

        await GrayMintApp.RunAsync(webApp, args);

    }
}