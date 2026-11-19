namespace GrayMint.Common.OpenApi;

/// <summary>
/// A controller's section in the API's description: the name a reader sees, and its place among the sections.
/// </summary>
/// <remarks>
/// The section's tag keeps the controller's name, which generated clients are named by; the reference shows
/// <paramref name="name"/> in its place. Sections with a place come first, by <paramref name="order"/>, and any other
/// after them, by name.
/// </remarks>
/// <param name="name">The section's name, as a reader sees it.</param>
/// <param name="order">The section's place: a lower one comes first.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiSectionAttribute(string name, int order) : Attribute
{
    /// <summary>The section's name, as a reader sees it.</summary>
    public string Name { get; } = name;

    /// <summary>The section's place: a lower one comes first.</summary>
    public int Order { get; } = order;
}
