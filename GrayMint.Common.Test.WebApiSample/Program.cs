using GrayMint.Common.AspNetCore;
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
        builder.RegisterAppCommonServices(new RegisterServicesOptions());
        builder.Services.AddSimpleRoleAuthorization(builder.Configuration.GetSection("Auth"));
        builder.Services.RegisterSimpleUserProvider(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));
        builder.Services.AddDbContext<WebApiSampleDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));

        // Add services to the container.
        var webApp = builder.Build();
        webApp.UseAppCommonServices(new UseServicesOptions());
        await AppCommon.CheckDatabaseCommand<WebApiSampleDbContext>(webApp, args);
        await webApp.UseSimpleUserProvider();

        await AppCommon.RunAsync(webApp, args);

    }
}