using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

/// <summary>
/// BID-13: rate limit per user per auction.
/// </summary>
public class BidRateLimitServiceTests
{
    [Fact]
    public async Task CheckAsync_WithinLimit_AllowsBid()
    {
        var service = CreateService(maxPerUser: 3);

        for (var i = 0; i < 3; i++)
        {
            var result = await service.CheckAsync(auctionId: 1, bidderId: 1, ipAddress: "127.0.0.1");
            Assert.True(result.IsAllowed);
        }
    }

    [Fact]
    public async Task CheckAsync_ExceedsUserLimit_BlocksBid()
    {
        var service = CreateService(maxPerUser: 2);

        Assert.True((await service.CheckAsync(1, 1, "127.0.0.1")).IsAllowed);
        Assert.True((await service.CheckAsync(1, 1, "127.0.0.1")).IsAllowed);

        var blocked = await service.CheckAsync(1, 1, "127.0.0.1");

        Assert.False(blocked.IsAllowed);
        Assert.Contains("exceeded", blocked.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckAsync_WhenDisabled_AlwaysAllows()
    {
        var service = CreateService(maxPerUser: 1, enabled: false);

        Assert.True((await service.CheckAsync(1, 1, "127.0.0.1")).IsAllowed);
        Assert.True((await service.CheckAsync(1, 1, "127.0.0.1")).IsAllowed);
    }

    private static BidRateLimitService CreateService(int maxPerUser, bool enabled = true)
    {
        var settings = Options.Create(new BidFraudDetectionSettings
        {
            Enabled = enabled,
            RateLimitingEnabled = enabled,
            MaxBidsPerMinutePerUser = maxPerUser,
            MaxBidsPerMinutePerAuction = 100
        });

        return new BidRateLimitService(
            new MemoryCache(new MemoryCacheOptions()),
            settings,
            new NoOpBidFraudAlertWriter(),
            NullLogger<BidRateLimitService>.Instance);
    }

    private sealed class NoOpBidFraudAlertWriter : IBidFraudAlertWriter
    {
        public Task<bool> CreateAlertAsync(
            int auctionId,
            long? bidId,
            int? userId,
            string alertType,
            string severity,
            string message,
            string? metadataJson,
            string? bidFlagReason,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
