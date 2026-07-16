using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace OnlineAuction.Services;

/// <summary>
/// Fixed-window counter backed by <see cref="IDistributedCache"/>.
/// Works with DistributedMemoryCache (single instance) or Redis (multi-instance).
/// </summary>
public static class DistributedFixedWindowCounter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> IncrementAsync(
        IDistributedCache cache,
        string key,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existingBytes = await cache.GetAsync(key, cancellationToken);
        WindowEntry entry;

        if (existingBytes is null
            || !TryDeserialize(existingBytes, out entry)
            || entry.ExpiresAtUtc <= now)
        {
            entry = new WindowEntry(1, now.Add(window));
        }
        else
        {
            entry = entry with { Count = entry.Count + 1 };
        }

        var ttl = entry.ExpiresAtUtc - now;
        if (ttl < TimeSpan.FromSeconds(1))
        {
            ttl = TimeSpan.FromSeconds(1);
        }

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, JsonOptions));
        await cache.SetAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return entry.Count;
    }

    private static bool TryDeserialize(byte[] bytes, out WindowEntry entry)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<WindowEntry>(Encoding.UTF8.GetString(bytes), JsonOptions);
            if (parsed is null)
            {
                entry = default!;
                return false;
            }

            entry = parsed;
            return true;
        }
        catch (JsonException)
        {
            entry = default!;
            return false;
        }
    }

    private sealed record WindowEntry(int Count, DateTimeOffset ExpiresAtUtc);
}
