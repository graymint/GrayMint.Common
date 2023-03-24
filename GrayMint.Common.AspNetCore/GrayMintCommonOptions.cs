namespace GrayMint.Common.AspNetCore;

public class GrayMintCommonOptions
{
    public string AppName { get; set; } = default!;

    internal void Validate()
    {
        // Configure Settings
        if (string.IsNullOrWhiteSpace(AppName))
            GrayMintApp.ThrowOptionsValidationException(nameof(AppName), typeof(string));
    }
}