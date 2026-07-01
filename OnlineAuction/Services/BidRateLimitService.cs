using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class BidRateLimitService : IBidRateLimitService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly IMemoryCache _memoryCache;
    private readonly BidFraudDetectionSettings _settings;
    private readonly IBidFraudAlertWriter _alertWriter;
    private readonly ILogger<BidRateLimitService> _logger;

    public BidRateLimitService(
        IMemoryCache memoryCache,
        IOptions<BidFraudDetectionSettings> settings,
        IBidFraudAlertWriter alertWriter,
        ILogger<BidRateLimitService> logger)
    {
        _memoryCache = memoryCache;
        _settings = settings.Value;
        _alertWriter = alertWriter;
        _logger = logger;
    }

    public async Task<BidRateLimitResult> CheckAsync(
        int auctionId,
        int bidderId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || !_settings.RateLimitingEnabled)
        {
            return new BidRateLimitResult(true);
        }

        var userKey = $"bid-rate:user:{auctionId}:{bidderId}";
        var auctionKey = $"bid-rate:auction:{auctionId}";

        var userCount = Increment(userKey);
        var auctionCount = Increment(auctionKey);

        var exceededByUser = userCount > _settings.MaxBidsPerMinutePerUser;
        var exceededByAuction = auctionCount > _settings.MaxBidsPerMinutePerAuction;
        if (!exceededByUser && !exceededByAuction)
        {
            return new BidRateLimitResult(true);
        }

        var reason = exceededByUser
            ? $"User exceeded {_settings.MaxBidsPerMinutePerUser} bids per minute for auction."
            : $"Auction exceeded {_settings.MaxBidsPerMinutePerAuction} bids per minute.";

        _logger.LogWarning(
            "Bid rate limit exceeded for auction {AuctionId} by user {UserId} from IP {IpAddress}. Reason: {Reason}",
            auctionId,
            bidderId,
            ipAddress,
            reason);

        await _alertWriter.CreateAlertAsync(
            auctionId,
            bidId: null,
            userId: bidderId,
            FraudAlertTypes.RateLimitExceeded,
            FraudAlertSeverities.Medium,
            $"Rate limit exceeded by user #{bidderId} on auction #{auctionId}.",
            JsonSerializer.Serialize(new { ip = ipAddress, reason, userCount, auctionCount }),
            bidFlagReason: null,
            cancellationToken);

        return new BidRateLimitResult(false, reason);
    }

    private int Increment(string key)
    {
        var counter = _memoryCache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return new RateLimitCounter();
        })!;

        lock (counter)
        {
            counter.Count++;
            return counter.Count;
        }
    }

    private sealed class RateLimitCounter
    {
        public int Count { get; set; }
    }
}
