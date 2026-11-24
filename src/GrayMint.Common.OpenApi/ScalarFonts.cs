using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;

namespace GrayMint.Common.OpenApi;

// Scalar's page in its own fonts, Inter and JetBrains Mono, served by this service: Scalar's option for them loads them
// from Scalar's CDN, so every reader's browser would call Scalar. The files are Fontsource's builds of both, under the
// SIL Open Font License (Fonts/*-OFL.txt), cut by script as Google Fonts cuts them.
internal static class ScalarFonts
{
    // relative to Scalar's page, as its own scripts are, so the urls hold under a path base or behind a proxy's prefix
    private const string Path = "fonts";
    private const string ResourcePrefix = "GrayMint.Common.OpenApi.Fonts.";
    private const string Inter = "Inter";
    private const string JetBrainsMono = "JetBrains Mono";

    private const string CyrillicExt = "U+0460-052F, U+1C80-1C8A, U+20B4, U+2DE0-2DFF, U+A640-A69F, U+FE2E-FE2F";
    private const string Cyrillic = "U+0301, U+0400-045F, U+0490-0491, U+04B0-04B1, U+2116";
    private const string GreekExt = "U+1F00-1FFF";
    private const string Greek = "U+0370-0377, U+037A-037F, U+0384-038A, U+038C, U+038E-03A1, U+03A3-03FF";

    private const string Vietnamese =
        "U+0102-0103, U+0110-0111, U+0128-0129, U+0168-0169, U+01A0-01A1, U+01AF-01B0, U+0300-0301, U+0303-0304, " +
        "U+0308-0309, U+0323, U+0329, U+1EA0-1EF9, U+20AB";

    private const string LatinExt =
        "U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, " +
        "U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C0, U+2113, U+2C60-2C7F, U+A720-A7FF";

    private const string Latin =
        "U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, " +
        "U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD";

    private static readonly IReadOnlyList<ScalarFontFace> FontFaces =
    [
        new(Inter, "100 900", "inter-cyrillic-ext-wght-normal.woff2", CyrillicExt),
        new(Inter, "100 900", "inter-cyrillic-wght-normal.woff2", Cyrillic),
        new(Inter, "100 900", "inter-greek-ext-wght-normal.woff2", GreekExt),
        new(Inter, "100 900", "inter-greek-wght-normal.woff2", Greek),
        new(Inter, "100 900", "inter-vietnamese-wght-normal.woff2", Vietnamese),
        new(Inter, "100 900", "inter-latin-ext-wght-normal.woff2", LatinExt),
        new(Inter, "100 900", "inter-latin-wght-normal.woff2", Latin),
        new(JetBrainsMono, "100 800", "jetbrains-mono-cyrillic-ext-wght-normal.woff2", CyrillicExt),
        new(JetBrainsMono, "100 800", "jetbrains-mono-cyrillic-wght-normal.woff2", Cyrillic),
        new(JetBrainsMono, "100 800", "jetbrains-mono-greek-wght-normal.woff2", Greek),
        new(JetBrainsMono, "100 800", "jetbrains-mono-vietnamese-wght-normal.woff2", Vietnamese),
        new(JetBrainsMono, "100 800", "jetbrains-mono-latin-ext-wght-normal.woff2", LatinExt),
        new(JetBrainsMono, "100 800", "jetbrains-mono-latin-wght-normal.woff2", Latin)
    ];

    // Scalar's page already asks for these families first, so naming their files is all it takes
    public static string Css { get; } = string.Join('\n', FontFaces.Select(x => x.ToCss(Path)));

    public static void MapScalarFonts(this IEndpointRouteBuilder app, string scalarPrefix)
    {
        // read as the service starts, so a font missing from the package stops the start, not the page
        var files = FontFaces.ToDictionary(x => x.FileName, x => ReadFile(x.FileName));

        // nothing in a font is private, so a page whose reader signed in loads them as well as one that is open
        app.MapGet($"{scalarPrefix}/{Path}/{{fileName}}", (string fileName, HttpResponse response) =>
            {
                if (!files.TryGetValue(fileName, out var file))
                    return Results.NotFound();

                // as Scalar's own files: the browser keeps it and asks each time whether it changed
                response.Headers.CacheControl = "no-cache";
                return Results.Bytes(file.Content, "font/woff2", entityTag: file.EntityTag);
            })
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static ScalarFontFile ReadFile(string fileName)
    {
        var assembly = typeof(ScalarFonts).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName)
                           ?? throw new InvalidOperationException(
                               $"Could not find the font {fileName} in {assembly.GetName().Name}.");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var content = memoryStream.ToArray();
        var hash = Convert.ToHexStringLower(SHA256.HashData(content).AsSpan(0, 8));
        return new ScalarFontFile(content, new EntityTagHeaderValue($"\"{hash}\""));
    }
}
