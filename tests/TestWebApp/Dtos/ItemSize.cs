using System.Text.Json.Serialization;

namespace TestWebApp.Dtos;

/// <summary>
/// How big an item is.
/// </summary>
// by name, as an app's enums are: a client is generated from the document (ApiClientGeneratorTest)
[JsonConverter(typeof(JsonStringEnumConverter<ItemSize>))]
public enum ItemSize
{
    /// <summary>Fits in a hand.</summary>
    Small,

    /// <summary>Takes two hands.</summary>
    Large
}
