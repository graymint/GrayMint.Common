using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public class AppAuthSettings
{
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public string BotIssuer { get; set; } = default!;
    public byte[] BotKey { get; set; } = default!;

    internal void Validate(bool isProduction)
    {
        // Configure Settings
        const string msg = "The App in AppSettings must be initialized properly. OptionName: {0}";

        var propertyName = nameof(BotIssuer);
        if (string.IsNullOrWhiteSpace(BotIssuer))
            throw new OptionsValidationException(propertyName, typeof(string), new[] { string.Format(msg, propertyName) });

        propertyName = nameof(BotKey);
        if (BotKey == null! || BotKey.Length == 0)
            throw new OptionsValidationException(propertyName, typeof(string), new[] { string.Format(msg, propertyName) });

        if (isProduction && BotKey.All(x => x == 0))
            throw new OptionsValidationException(propertyName, typeof(string), new[]
            {
                string.Format(msg, propertyName),"The given key is not acceptable in production."
            });
    }
}