using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace TestWebApp.Dtos.Orders;

// by name, with a member's [EnumMember] value, which NSwag took and the serializer does not
[JsonConverter(typeof(JsonStringEnumConverter<OrderChannel>))]
public enum OrderChannel
{
    [EnumMember(Value = "web-shop")]
    Web,
    Phone
}
