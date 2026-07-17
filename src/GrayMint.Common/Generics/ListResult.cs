namespace GrayMint.Common.Generics;

public sealed class ListResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
}
