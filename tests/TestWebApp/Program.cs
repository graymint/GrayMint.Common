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
        JobRunner.FastInstance.MaxDegreeOfParallelism = 2;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrayMintCommonServices(new RegisterServicesOptions());
        builder.Services.AddGrayMintSwagger();
        builder.Services.AddGrayMintJob<MyJobService>(new GrayMintJobOptions
            { Interval = TimeSpan.FromSeconds(5), DueTime = TimeSpan.Zero, Name = "MyJobService" });
        builder.Services.AddGrayMintJob<MyJobService2>(new GrayMintJobOptions { Interval = TimeSpan.FromSeconds(10) });
        builder.Services.AddScoped<MyJobService>();
        builder.Services.AddScoped<MyJobService2>();

        var app = builder.Build();
        app.UseGrayMintCommonServices(new UseServicesOptions());
        app.UseGrayMintSwagger(new UseSwaggerOptions { RedirectRootToSwaggerUi = true });


        return app.RunAsync();
    }
}