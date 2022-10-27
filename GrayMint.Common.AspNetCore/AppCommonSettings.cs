namespace GrayMint.Common.AspNetCore;

public class AppCommonSettings
{
    public string AppName { get; set; } = default!;
    public string AuthKey { get; set; } = default!; //todo deprecated
    public string AuthIssuer { get; set; } = default!; //todo deprecated
    public static string LegacyAuthScheme { get; set; } = "LegacyAuthScheme"; //todo deprecated


    internal void Validate()
    {
        // Configure Settings
        if (string.IsNullOrWhiteSpace(AppName))
            AppCommon.ThrowOptionsValidationException(nameof(AppName), typeof(string));
    }
}