namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotAuthenticationOptions
{
    public required string BotIssuer { get; set; }
    public string? BotAudience { get; set; }
    public required byte[] BotKey { get; set; }
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public void Validate(bool isProduction)
    {
        if (string.IsNullOrEmpty(BotIssuer))
            GrayMintApp.ThrowOptionsValidationException(nameof(BotIssuer), typeof(string));

        if (BotKey == null! || BotKey.Length == 0)
            GrayMintApp.ThrowOptionsValidationException(nameof(BotKey), typeof(byte[]));

        if (isProduction && BotKey!.All(x => x == 0))
            GrayMintApp.ThrowOptionsValidationException(nameof(BotKey), typeof(byte[]), "This Key is not valid for Production.");
    }
}