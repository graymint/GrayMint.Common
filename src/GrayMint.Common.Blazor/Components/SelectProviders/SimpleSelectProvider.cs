using GrayMint.Common.Blazor.Components.GenericSelects;

namespace GrayMint.Common.Blazor.Components.SelectProviders;

public abstract class SimpleSelectProvider(GenericSelectItem? preload, string? excludeId = null)
    : IGenericSelectProvider
{
    private GenericSelectItem? _cacheItemId;

    // the apps' own providers use them
    // ReSharper disable UnusedParameter.Global
    protected abstract Task<IReadOnlyList<GenericSelectItem>> SearchImpl(string? search, int? recordCount,
        CancellationToken cancellationToken);

    protected abstract Task<GenericSelectItem> GetImpl(string itemId, CancellationToken cancellationToken);
    // ReSharper restore UnusedParameter.Global
    public virtual Task<GenericSelectItem?> AddNew() => throw new NotImplementedException();
    public virtual int SearchModeThreshold => 50;
    public virtual bool CanAdd => false;

    public async Task<GenericSelectItem> Get(string itemId, CancellationToken cancellationToken)
    {
        if (preload != null && preload.ItemId == itemId)
            return preload;

        if (_cacheItemId?.ItemId == itemId)
            return _cacheItemId;

        var item = await GetImpl(itemId, cancellationToken);
        if (item.ItemId == excludeId)
            throw new KeyNotFoundException();

        _cacheItemId = item;
        return item;
    }

    public async Task<IReadOnlyList<GenericSelectItem>> Search(string? search, int? recordCount,
        CancellationToken cancellationToken)
    {
        var items = await SearchImpl(search: search, recordCount: recordCount, cancellationToken);
        return items.Where(x => x.ItemId != excludeId).ToArray();
    }
}