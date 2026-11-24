using Microsoft.Net.Http.Headers;

namespace GrayMint.Common.OpenApi;

internal sealed record ScalarFontFile(ReadOnlyMemory<byte> Content, EntityTagHeaderValue EntityTag);
