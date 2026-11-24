using Microsoft.AspNetCore.Components;
using MudBlazor;
using GrayMint.Common.Blazor.Components.SelectProviders;

namespace GrayMint.Common.Blazor.Components.GenericSelects;

public partial class GenericSelect : SuperComponentBase
{
    [Parameter] public string? Class { get; set; } = "pt-1";
    [Parameter] public string? Style { get; set; } = "pt-1";
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool ShowImage { get; set; }
    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public string? InputClass { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public EventCallback OnItemAdded { get; set; }
    [Parameter] public Margin Margin { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? HelperText { get; set; }
    [Parameter] public bool HelperTextOnFocus { get; set; }
    [Parameter] public IGenericSelectProvider Provider { get; init; } = null!;

    private readonly GenericSelectItem _separatorSelectItem = new("{7B86DCAE-8128-42E2-9116-4976EA68A6A7}", "-");
    private readonly GenericSelectItem _newSelectItem = new("{B3BE2231-3996-4BAF-8AF1-85CC3909EB6A}", CommonResource.AddNew);
    private GenericSelectItem? _selectedItem;
    private MudSelect<GenericSelectItem>? _selectControl;
    private IReadOnlyList<GenericSelectItem> _items = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await Refresh();
    }

    private async Task Refresh()
    {
        _items = (await OnSearchInternal(null, CancellationToken.None)).ToArray();
        _selectedItem = Value != null ? _items.FirstOrDefault(x => x.ItemId == Value) ?? await FindItem(Value) : null;
        StateHasChanged();
    }

    private async Task<GenericSelectItem?> FindItem(string itemId)
    {
        try
        {
            return await Provider.Get(itemId, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private void AddControlItems(List<GenericSelectItem> items)
    {
        if (Provider.CanAdd)
        {
            if (items.Count > 0) items.Add(_separatorSelectItem);
            items.Add(_newSelectItem);
        }
    }

    private bool ItemDisabledFunc(GenericSelectItem? arg)
    {
        return arg == _separatorSelectItem;
    }

    private void OnClearButtonClick()
    {
        _selectedItem = null;
        if (!Required)
            ValueChanged.InvokeAsync(null);
    }

    private void OnListValueChanged(GenericSelectItem? selectItem)
    {
        if (selectItem == _newSelectItem)
        {
            _selectControl?.CloseMenu();
            _ = OnAddNewItem();
            return;
        }

        _selectedItem = selectItem;
        ValueChanged.InvokeAsync(selectItem?.ItemId);
    }

    private async Task OnAddNewItem()
    {
        _selectedItem = await Provider.AddNew();
        if (_selectedItem != null)
        {
            await ValueChanged.InvokeAsync(_selectedItem.ItemId);
            await OnItemAdded.InvokeAsync();
            await Refresh();
        }
    }

    private async Task<IEnumerable<GenericSelectItem>> OnSearch(string? value, CancellationToken cancellationToken)
    {
        await Task.Delay(700, cancellationToken);
        return await OnSearchInternal(value, cancellationToken);
    }

    private async Task<IEnumerable<GenericSelectItem>> OnSearchInternal(string? value, CancellationToken cancellationToken)
    {
        value = value?.Trim();
        var searchResults = await Provider.Search(value, Provider.SearchModeThreshold, cancellationToken);
        var items = new List<GenericSelectItem>();

        items.AddRange(searchResults);
        AddControlItems(items);
        return items;
    }
}