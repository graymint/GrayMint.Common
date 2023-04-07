using GrayMint.Common.AspNetCore.SimpleUserControllers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore.SimpleUserControllers;

public static class SimpleUserControllersExtension
{
    public static void AddGrayMintSimpleUserController(this IServiceCollection services,
        SimpleUserControllerOptions? options = null)
    {
        options ??= new SimpleUserControllerOptions();
        services.AddSingleton(Options.Create(options));
        services.AddScoped<UserService>();
        services.AddScoped<TeamService>();
    }
}