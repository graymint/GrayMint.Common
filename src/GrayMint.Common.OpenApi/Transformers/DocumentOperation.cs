using Microsoft.OpenApi;

namespace GrayMint.Common.OpenApi.Transformers;

internal sealed class DocumentOperation
{
    public required string Path { get; init; }
    public required HttpMethod Method { get; init; }
    public required OpenApiOperation Operation { get; init; }
}
