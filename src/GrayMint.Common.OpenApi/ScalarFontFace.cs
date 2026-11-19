namespace GrayMint.Common.OpenApi;

// one script of a font, in its own file, which a browser loads only for a page that shows that script
internal sealed record ScalarFontFace(string Family, string Weight, string FileName, string UnicodeRange)
{
    public string ToCss(string path)
    {
        return $$"""
            @font-face {
              font-family: "{{Family}}";
              font-style: normal;
              font-weight: {{Weight}};
              font-display: swap;
              src: url("{{path}}/{{FileName}}") format("woff2");
              unicode-range: {{UnicodeRange}};
            }
            """;
    }
}
