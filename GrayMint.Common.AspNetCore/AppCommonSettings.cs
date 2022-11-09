namespace GrayMint.Common.AspNetCore;

public class AppCommonSettings
{
    public string AppName { get; set; } = default!;

    internal void Validate()
    {
        // Configure Settings
        if (string.IsNullOrWhiteSpace(AppName))
            AppCommon.ThrowOptionsValidationException(nameof(AppName), typeof(string));
    }
}