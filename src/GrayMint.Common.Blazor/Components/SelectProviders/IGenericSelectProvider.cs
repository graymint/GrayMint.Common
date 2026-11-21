using GrayMint.Common.Blazor.Components.GenericSelects;

namespace GrayMint.Common.Blazor.Components.SelectProviders;

public interface IGenericSelectProvider
{
    public int SearchModeThreshold { get; }
    Task<GenericSelectItem?> AddNew();
    Task<GenericSelectItem> Get(string itemId, CancellationToken cancellationToken);
    Task<GenericSelectItem[]> Search(string? search, int? recordCount, CancellationToken cancellationToken);
    bool CanAdd { get; }

}
