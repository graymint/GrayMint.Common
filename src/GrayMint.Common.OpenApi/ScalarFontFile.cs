using Microsoft.Net.Http.Headers;

namespace GrayMint.Common.OpenApi;

internal sealed record ScalarFontFile(byte[] Content, EntityTagHeaderValue EntityTag);
