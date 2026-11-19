namespace GrayMint.Common.Generics;

/// <summary>
/// One page of a list, and how many items match in all.
/// </summary>
public sealed class ListResult<T>
{
    /// <summary>The items of this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>How many items match, on every page together.</summary>
    public required int TotalCount { get; init; }
}
