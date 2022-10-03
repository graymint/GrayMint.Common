using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.SimpleUserManagement;
using GrayMint.Common.Test.WebApiSample.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.Test.WebApiSample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        AppCommon.RegisterServices(builder, new RegisterServicesOptions());
        SimpleUserProvider.RegisterSimpleUserProvider(builder, options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));
        builder.Services.AddDbContext<WebApiSampleDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("AppDatabase")));

        // Add services to the container.
        var webApp = builder.Build();
        AppCommon.UseServices(webApp, new UseServicesOptions());
        await AppCommon.CheckDatabaseCommand<WebApiSampleDbContext>(webApp, args);
        await SimpleUserProvider.UseSimpleUserProvider(webApp);

        await AppCommon.RunAsync(webApp, args);

    }
}