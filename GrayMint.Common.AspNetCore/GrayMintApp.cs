using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public static class GrayMintApp
{
    public const string CorsPolicyName = "AllowAllCorsPolicy";
    public const string OptionsValidationMsgTemplate = "The AppSettings must be initialized properly. OptionName: {0}";

    public static OptionsValidationException CreateOptionsValidationException(string optionsName, Type optionsType,
        string? failureMessage = null)
    {
        var failureMessages = new[] { string.Format(OptionsValidationMsgTemplate, optionsName) };
        if (failureMessage != null) failureMessages = failureMessages.Concat(new[] { failureMessage }).ToArray();
        return new OptionsValidationException(optionsName, optionsType, failureMessages);
    }

    public static void ThrowOptionsValidationException(string optionsName, Type optionsType, string? failureMessage = null)
    {
        throw CreateOptionsValidationException(optionsName, optionsType, failureMessage);
    }

    public static async Task UseGrayMintDatabaseCommand<T>(this IServiceProvider serviceProvider, string[] args) where T : DbContext
    {
        var logger = serviceProvider.GetRequiredService<ILogger<WebApplication>>();

        await using var scope = serviceProvider.CreateAsyncScope();
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