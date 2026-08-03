using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class BidShadowBanService : IBidShadowBanService
{
    private readonly IDistributedCache _cache;
    private readonly BidFraudDetectionSettings _settings;
    private readonly ILogger<BidShadowBanService> _logger;

    public BidShadowBanService(
        IDistributedCache cache,
        IOptions<BidFraudDetectionSettings> settings,
        ILogger<BidShadowBanService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> IsShadowBannedAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_settings.HighSeverityAction, HighSeverityBidActions.ShadowBan, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = await _cache.GetStringAsync(Key(userId), cancellationToken);
        return !string.IsNullOrEmpty(value);
    }

    public async Task ApplyShadowBanAsync(
        int userId,
        TimeSpan duration,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (duration <= TimeSpan.Zero)
        {
            duration = TimeSpan.FromMinutes(1);
        }

        await _cache.SetStringAsync(
            Key(userId),
            reason,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration },
            cancellationToken);

        _logger.LogWarning(
            "Shadow-ban applied for user {UserId} for {DurationMinutes} minutes. Reason: {Reason}",
            userId,
            duration.TotalMinutes,
            reason);
    }

    private static string Key(int userId) => $"bid-shadow-ban:{userId}";
}
