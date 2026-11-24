using GrayMint.Common.Blazor.Components.GenericSelects;

namespace GrayMint.Common.Blazor.Components.SelectProviders;

public class StaticSelectProvider(IReadOnlyList<GenericSelectItem> items)
    : SimpleSelectProvider(preload: null, excludeId: null)
{
    protected override Task<IReadOnlyList<GenericSelectItem>> SearchImpl(string? search, int? recordCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GenericSelectItem> result = items
            .Where(x => string.IsNullOrWhiteSpace(search) || search.Equals(x.Text, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(result);
    }

    protected override Task<GenericSelectItem> GetImpl(string itemId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.Single(x => x.ItemId == itemId));
    }
}