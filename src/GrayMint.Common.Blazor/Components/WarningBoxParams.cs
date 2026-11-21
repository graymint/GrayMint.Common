namespace GrayMint.Common.Blazor.Components;

public class WarningBoxParams
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string YesButtonText { get; set; } = CommonResource.Yes;
    public string NoButtonText { get; set; } = CommonResource.No;
    public bool ShowNoButton { get; set; } = true;
    public IEnumerable<string>? Warnings { get; set; }
}