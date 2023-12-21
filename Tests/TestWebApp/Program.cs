
using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.Jobs;
using GrayMint.Common.JobController;
using GrayMint.Common.Swagger;
using TestWebApp.Services;

namespace TestWebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrayMintCommonServices(new RegisterServicesOptions());
        builder.Services.AddGrayMintSwagger("Common.Test", true);
        builder.Services.AddGrayMintJob<MyJobService>(new JobConfig { Interval = TimeSpan.FromSeconds(1) });
        builder.Services.AddGrayMintJob<MyJobService2>(new JobConfig { Interval = TimeSpan.FromSeconds(1) });
         builder.Services.AddScoped<MyJobService>();
         builder.Services.AddScoped<MyJobService2>();

        var app = builder.Build();
        app.UseGrayMintCommonServices(new UseServicesOptions());
        app.UseGrayMintSwagger(true);
            
        app.Run();
    }
}