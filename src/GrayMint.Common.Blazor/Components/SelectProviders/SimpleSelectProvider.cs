using GrayMint.Common.Blazor.Components.GenericSelects;

namespace GrayMint.Common.Blazor.Components.SelectProviders;

public abstract class SimpleSelectProvider(GenericSelectItem? preload, string? excludeId = null)
    : IGenericSelectProvider
{
    protected abstract Task<GenericSelectItem[]> SearchImpl(string? search, int? recordCount, CancellationToken cancellationToken);
    protected abstract Task<GenericSelectItem> GetImpl(string itemId, CancellationToken cancellationToken);
    public virtual Task<GenericSelectItem?> AddNew() => throw new NotImplementedException();
    public virtual int SearchModeThreshold => 50;
    public virtual bool CanAdd => false;
    private GenericSelectItem? _cacheItemId;

    public async Task<GenericSelectItem> Get(string itemId, CancellationToken cancellationToken)
    {
        if (preload != null && preload.ItemId == itemId)
            return preload;

        // Check cache
        if (_cacheItemId?.ItemId == itemId)
            return _cacheItemId;

        // Fetch item
        var item = await GetImpl(itemId, cancellationToken);
        if (item.ItemId == excludeId)
            throw new KeyNotFoundException();

        _cacheItemId = item;
        return item;
    }

    public async Task<GenericSelectItem[]> Search(string? search, int? recordCount, CancellationToken cancellationToken)
    {
        var items = await SearchImpl(search: search, recordCount: recordCount, cancellationToken);
        items = items.Where(x => x.ItemId != excludeId).ToArray();
        return items.ToArray();
    }
}