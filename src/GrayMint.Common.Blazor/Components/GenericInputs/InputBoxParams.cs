using MudBlazor;
using GrayMint.Common.Blazor.Components.SelectProviders;

namespace GrayMint.Common.Blazor.Components.GenericInputs;

public class InputBoxParams<T>
{
    public string? Title { get; set; }
    public T Value { get; set; } = default!;
    public string? Label { get; set; }
    public bool Readonly { get; set; }
    public bool Required { get; set; } = true;
    public string? Description { get; set; }
    public string? HelperText { get; set; }
    public string? PlaceHolder { get; set; }
    public int? Lines { get; set; }
    public IGenericSelectProvider? SelectProvider { get; set; }
    public string NullText { get; set; } = CommonResource.NotSet;
    public string TrueText { get; set; } = CommonResource.Yes;
    public string FalseText { get; set; } = CommonResource.No;
    public bool IsCode { get; set; }
    public bool? ShowCopyTextButton { get; set; }
    public bool ShowInputLabel { get; set; }
    public bool? Wrap { get; set; }
    public string? DownloadFileName { get; set; }
    public bool HelperTextOnFocus { get; set; }
    public MaxWidth? MaxWidth { get; set; } = MudBlazor.MaxWidth.ExtraSmall;
    public bool CancelIfNoChange { get; set; } = true;
    public IEnumerable<string>? Warnings { get; set; }
    public object? Validation { get; set; }
    public string OkButtonText { get; set; } = CommonResource.OK;
}