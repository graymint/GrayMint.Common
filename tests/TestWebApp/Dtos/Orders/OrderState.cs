using System.Text.Json.Serialization;

namespace TestWebApp.Dtos.Orders;

// by name, as an app's enums are: a client is generated from the document (ApiClientGeneratorTest)
[JsonConverter(typeof(JsonStringEnumConverter<OrderState>))]
public enum OrderState
{
    Open,
    Closed
}
