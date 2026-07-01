using System.Text;
using System.Text.Json;

namespace OnlineAuction.Messaging;

internal static class RabbitMqJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static byte[] Serialize<T>(T value) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options));

    public static T? Deserialize<T>(ReadOnlySpan<byte> body) =>
        JsonSerializer.Deserialize<T>(body, Options);
}
