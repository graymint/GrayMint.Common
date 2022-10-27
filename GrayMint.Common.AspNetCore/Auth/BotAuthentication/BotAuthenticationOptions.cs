namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotAuthenticationOptions
{
    public string BotIssuer { get; set; } = default!;
    public string? BotAudience { get; set; }
    public byte[] BotKey { get; set; } = default!;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public void Validate(bool isProduction)
    {
        if (string.IsNullOrEmpty(BotIssuer))
            AppCommon.ThrowOptionsValidationException(nameof(BotIssuer), typeof(string));

        if (BotKey == null! || BotKey.Length == 0)
            AppCommon.ThrowOptionsValidationException(nameof(BotKey), typeof(byte[]));

        if (isProduction && BotKey!.All(x => x == 0))
            AppCommon.ThrowOptionsValidationException(nameof(BotKey), typeof(byte[]), "This Key is not valid for Production.");
    }
}