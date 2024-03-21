
using GrayMint.Common.AspNetCore;
using GrayMint.Common.AspNetCore.Jobs;
using GrayMint.Common.Jobs;
using GrayMint.Common.Swagger;
using TestWebApp.Services;

namespace TestWebApp;

public class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrayMintCommonServices(new RegisterServicesOptions());
        builder.Services.AddGrayMintSwagger("Common.Test", true);
        builder.Services.AddGrayMintJob<MyJobService>(new GrayMintJobOptions { Interval = TimeSpan.FromSeconds(5), DueTime = TimeSpan.Zero, Name = "MyJobService" }, 1);
        builder.Services.AddGrayMintJob<MyJobService2>(new GrayMintJobOptions { Interval = TimeSpan.FromSeconds(10) }, 2);
        builder.Services.AddScoped<MyJobService>();
        builder.Services.AddScoped<MyJobService2>();

        var app = builder.Build();
        app.UseGrayMintCommonServices(new UseServicesOptions());
        app.UseGrayMintSwagger(true);

        JobRunner.Default.MaxDegreeOfParallelism = 2;

        return app.RunAsync();
    }
}