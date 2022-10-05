using Microsoft.Extensions.Options;

namespace GrayMint.Common.AspNetCore;

public class AppCommonSettings
{
    public string AppName { get; set; } = default!;
    
    public string AuthKey { get; set; } = default!; //todo deprecated
    public string AuthIssuer { get; set; } = default!; //todo deprecated
    public static string LegacyAuthScheme { get; set; } = "LegacyAuthScheme";


    internal void Validate()
    {
        // Configure Settings
        const string msg = "The App in AppSettings must be initialized properly. OptionName: {0}";

        var propertyName = nameof(AppName);
        if (string.IsNullOrWhiteSpace(AppName))
            throw new OptionsValidationException(propertyName, typeof(string), new[] { string.Format(msg, propertyName) });
    }
}