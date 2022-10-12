using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore;

public static class AppCommon
{
    public const string CorsPolicyName = "AllowAllCorsPolicy";
    public const string OptionsValidationMsgTemplate = "The App in AppSettings must be initialized properly. OptionName: {0}";

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