using System.Reflection;
using Namotion.Reflection;

namespace GrayMint.Common.OpenApi.Transformers;

// The doc comments an app and its packages ship beside their assemblies, as Markdown. A generic type's closed form,
// such as ListResult<Item>, has the generic type's comments.
internal static class XmlDocs
{
    private static readonly XmlDocsOptions Options = new() { FormattingMode = XmlDocsFormattingMode.Markdown };

    public static string? Summary(MemberInfo member)
    {
        return NullIfEmpty(member.GetXmlDocsSummary(Options));
    }

    public static string? Remarks(MemberInfo member)
    {
        return NullIfEmpty(member.GetXmlDocsRemarks(Options));
    }

    public static string? Returns(MethodInfo method)
    {
        return NullIfEmpty(method.GetXmlDocsTag("returns", Options));
    }

    public static string? Parameter(ParameterInfo? parameter)
    {
        return parameter == null ? null : NullIfEmpty(parameter.GetXmlDocs(Options));
    }

    public static string? Join(params string?[] parts)
    {
        var text = string.Join("\n\n", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        return text.Length == 0 ? null : text;
    }

    private static string? NullIfEmpty(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
