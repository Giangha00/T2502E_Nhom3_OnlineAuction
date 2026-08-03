using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class BidRateLimitServiceTests
{
    [Fact]
    public async Task CheckAsync_WhenRateLimitingDisabled_AllowsBid()
    {
        var service = CreateService(
            new BidFraudDetectionSettings { Enabled = true, RateLimitingEnabled = false });

        var result = await service.CheckAsync(auctionId: 1, bidderId: 42, ipAddress: "127.0.0.1");

        Assert.True(result.IsAllowed);
        Assert.Equal(0, _alertWriter.CreateCount);
    }

    [Fact]
    public async Task CheckAsync_UnderUserLimit_AllowsBid()
    {
        var settings = new BidFraudDetectionSettings
        {
            Enabled = true,
            RateLimitingEnabled = true,
            MaxBidsPerMinutePerUser = 3,
            MaxBidsPerMinutePerAuction = 30,
            MaxBidsPerMinutePerIp = 20,
            ChallengeEnabled = false
        };

        var service = CreateService(settings);

        var result = await service.CheckAsync(auctionId: 5, bidderId: 7, ipAddress: "10.0.0.1");

        Assert.True(result.IsAllowed);
        Assert.Equal(1, result.UserCount);
        Assert.Equal(0, _alertWriter.CreateCount);
    }

    [Fact]
    public async Task CheckAsync_ExceedsUserLimit_BlocksAndCreatesAlert()
    {
        var settings = new BidFraudDetectionSettings
        {
            Enabled = true,
            RateLimitingEnabled = true,
            MaxBidsPerMinutePerUser = 2,
            MaxBidsPerMinutePerAuction = 100,
            MaxBidsPerMinutePerIp = 100,
            ChallengeEnabled = false
        };

        var service = CreateService(settings);

        await service.CheckAsync(1, 9, "192.168.0.1");
        await service.CheckAsync(1, 9, "192.168.0.1");
        var blocked = await service.CheckAsync(1, 9, "192.168.0.1");

        Assert.False(blocked.IsAllowed);
        Assert.Contains("User exceeded", blocked.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, _alertWriter.CreateCount);
    }

    private readonly FakeBidFraudAlertWriter _alertWriter = new();

    private BidRateLimitService CreateService(BidFraudDetectionSettings settings)
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new BidRateLimitService(
            cache,
            Options.Create(settings),
            _alertWriter,
            NullLogger<BidRateLimitService>.Instance);
    }

    private sealed class FakeBidFraudAlertWriter : IBidFraudAlertWriter
    {
        public int CreateCount { get; private set; }

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
        {
            CreateCount++;
            return Task.FromResult(true);
        }
    }
}
