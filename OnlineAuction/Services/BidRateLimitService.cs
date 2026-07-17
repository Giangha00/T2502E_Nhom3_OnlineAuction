using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Entities;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

/// <summary>
/// Bid rate limiting by user + auction + IP using <see cref="IDistributedCache"/>.
/// Default registration is DistributedMemoryCache (single-instance). For multi-instance,
/// replace with Redis (or another distributed store) in Program.cs — see docs.
/// </summary>
public sealed class BidRateLimitService : IBidRateLimitService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly IDistributedCache _cache;
    private readonly BidFraudDetectionSettings _settings;
    private readonly IBidFraudAlertWriter _alertWriter;
    private readonly ILogger<BidRateLimitService> _logger;

    public BidRateLimitService(
        IDistributedCache cache,
        IOptions<BidFraudDetectionSettings> settings,
        IBidFraudAlertWriter alertWriter,
        ILogger<BidRateLimitService> logger)
    {
        _cache = cache;
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
        var normalizedIp = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress.Trim();
        var ipKey = $"bid-rate:ip:{auctionId}:{normalizedIp}";

        var userCount = await DistributedFixedWindowCounter.IncrementAsync(
            _cache, userKey, Window, cancellationToken);
        var auctionCount = await DistributedFixedWindowCounter.IncrementAsync(
            _cache, auctionKey, Window, cancellationToken);
        var ipCount = await DistributedFixedWindowCounter.IncrementAsync(
            _cache, ipKey, Window, cancellationToken);

        var exceededByUser = userCount > _settings.MaxBidsPerMinutePerUser;
        var exceededByAuction = auctionCount > _settings.MaxBidsPerMinutePerAuction;
        var exceededByIp = ipCount > _settings.MaxBidsPerMinutePerIp;

        var requiresChallenge = _settings.ChallengeEnabled
            && !string.Equals(_settings.ChallengeProvider, BidChallengeProviders.None, StringComparison.OrdinalIgnoreCase)
            && userCount >= _settings.ChallengeAfterBidsPerMinute;

        if (!exceededByUser && !exceededByAuction && !exceededByIp)
        {
            return new BidRateLimitResult(
                true,
                RequiresChallenge: requiresChallenge,
                UserCount: userCount,
                AuctionCount: auctionCount,
                IpCount: ipCount);
        }

        var reason = exceededByUser
            ? $"User exceeded {_settings.MaxBidsPerMinutePerUser} bids per minute for auction."
            : exceededByIp
                ? $"IP exceeded {_settings.MaxBidsPerMinutePerIp} bids per minute for auction."
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
            JsonSerializer.Serialize(new { ip = ipAddress, reason, userCount, auctionCount, ipCount }),
            bidFlagReason: null,
            cancellationToken);

        return new BidRateLimitResult(
            false,
            reason,
            RequiresChallenge: requiresChallenge,
            UserCount: userCount,
            AuctionCount: auctionCount,
            IpCount: ipCount);
    }
}
