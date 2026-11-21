namespace GrayMint.Common.Blazor.Components.GenericSelects;

public class GenericSelectItem(string itemId, string? text)
{
    public string ItemId { get; } = itemId;
    public string? Text { get; } = text;
    public string? ImageUrl { get; init; }
    public string? Icon { get; init; }
    public string? Class { get; init; }
    public override string ToString()
    {
        return Text ?? ItemId;
    }

    public string CalcClass => "d-flex align-center " + Class;
}