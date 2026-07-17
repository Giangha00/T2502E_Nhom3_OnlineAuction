using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Messaging;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class BidServicePlaceBidTests
{
    private static DateTime UtcNow => DateTime.UtcNow;

    [Fact]
    public async Task PlaceBid_ValidAmount_UpdatesPriceAndHistory()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);

        Assert.True(result.Success);
        Assert.Equal(110m, result.CurrentPrice);
        Assert.Equal(1, result.BidCount);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.Equal(110m, auction!.CurrentPrice);

        var winningBid = await db.Bids.SingleAsync(bid => bid.IsWinning);
        Assert.Equal(110m, winningBid.Amount);
        Assert.Equal(1, winningBid.BidderId);
    }

    [Fact]
    public async Task PlaceBid_BelowMinimum_RejectsWithoutChangingPrice()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 109m);

        Assert.False(result.Success);
        Assert.Contains("Your bid must be at least", result.Message);
        Assert.Equal(0, await db.Bids.CountAsync());

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.Equal(100m, auction!.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBid_InvalidIncrement_Rejects()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 115m);

        Assert.False(result.Success);
        Assert.Contains("per step", result.Message);
        Assert.Empty(await db.Bids.ToListAsync());
    }

    [Fact]
    public async Task PlaceBid_PendingRegistration_BlocksBid()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 50m, bidStep: 5m);
        var service = CreateBidService(db, registrationBlockMessage: "Your registration is pending approval.");

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 55m);

        Assert.False(result.Success);
        Assert.Equal("Your registration is pending approval.", result.Message);
        Assert.Empty(await db.Bids.ToListAsync());
    }

    [Fact]
    public async Task PlaceBid_SellerSelfBid_Rejects()
    {
        await using var db = CreateDbContext();
        const int sellerId = 10;
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: sellerId, currentPrice: 80m, bidStep: 5m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: sellerId, amount: 85m);

        Assert.False(result.Success);
        Assert.Equal("You cannot bid on your own listing.", result.Message);
    }

    [Fact]
    public async Task PlaceBid_BeforeStartDate_Rejects()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(
            db,
            currentPrice: 100m,
            bidStep: 10m,
            startDate: UtcNow.AddHours(1),
            endDate: UtcNow.AddHours(2));
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);

        Assert.False(result.Success);
        Assert.Equal("The live auction has not started yet.", result.Message);
    }

    [Fact]
    public async Task PlaceBid_AfterEndDate_Rejects()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(
            db,
            currentPrice: 100m,
            bidStep: 10m,
            startDate: UtcNow.AddHours(-2),
            endDate: UtcNow.AddMinutes(-1));
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);

        Assert.False(result.Success);
        Assert.Equal("This auction has ended.", result.Message);
    }

    [Theory]
    [InlineData(AuctionStatuses.PendingReview, "pending review")]
    [InlineData(AuctionStatuses.AwaitingPayment, "ended")]
    [InlineData(AuctionStatuses.Completed, "completed")]
    public async Task PlaceBid_DisallowedStatus_Rejects(string status, string messageFragment)
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m, status: status);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains(messageFragment, result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBid_TwoBuyers_SecondHigherBidWins()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var first = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);
        var second = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 120m);

        Assert.True(first.Success);
        Assert.True(second.Success);

        var winningBid = await db.Bids.SingleAsync(bid => bid.IsWinning);
        Assert.Equal(2, winningBid.BidderId);
        Assert.Equal(120m, winningBid.Amount);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.Equal(120m, auction!.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBid_SameBuyerRaise_SucceedsAndUpdatesWinningBid()
    {
        await using var db = CreateDbContext();
        var auctionId = await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        Assert.True((await service.PlaceBidAsync(auctionId, 1, 110m)).Success);
        Assert.True((await service.PlaceBidAsync(auctionId, 1, 120m)).Success);

        var bids = await db.Bids.Where(bid => bid.AuctionId == auctionId).ToListAsync();
        Assert.Equal(2, bids.Count);
        Assert.Single(bids, bid => bid.IsWinning);
        Assert.Equal(120m, bids.Single(bid => bid.IsWinning).Amount);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.Equal(120m, auction!.CurrentPrice);
    }

    [Theory]
    [InlineData(0, 110)]
    [InlineData(-1, 110)]
    [InlineData(1, 0)]
    public async Task PlaceBid_InvalidInput_Rejects(int auctionId, decimal amount)
    {
        await using var db = CreateDbContext();
        await SeedLiveAuctionAsync(db, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: amount);

        Assert.False(result.Success);
        Assert.Equal("Invalid bid.", result.Message);
    }

    private static AuctionHouseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AuctionHouseDbContext(options);
    }

    private static BidService CreateBidService(
        AuctionHouseDbContext db,
        string? registrationBlockMessage = null)
    {
        var registrationService = new StubAuctionRegistrationService(registrationBlockMessage);
        var fraudSettings = Options.Create(new BidFraudDetectionSettings
        {
            Enabled = false,
            RateLimitingEnabled = false
        });

        return new BidService(
            db,
            registrationService,
            new NoOpBidFraudDetectionService(),
            new NoOpRabbitMqPublisher(),
            new NoOpServiceScopeFactory(),
            new NoOpHttpContextAccessor(),
            fraudSettings,
            NullLogger<BidService>.Instance);
    }

    private static async Task<int> SeedLiveAuctionAsync(
        AuctionHouseDbContext db,
        decimal currentPrice,
        decimal bidStep,
        int sellerId = 10,
        int auctionId = 100,
        string status = AuctionStatuses.Live,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        db.Users.AddRange(
            CreateUser(1, "buyer1@test.local"),
            CreateUser(2, "buyer2@test.local"),
            CreateUser(sellerId, "seller@test.local"));

        db.Categories.Add(new Category
        {
            Id = 1,
            Name = "Test",
            Slug = "test"
        });

        var product = new Product
        {
            Id = auctionId,
            SellerId = sellerId,
            CategoryId = 1,
            Name = "Test Card",
            PrimaryImage = "/img.png"
        };

        var auction = new Auction
        {
            Id = auctionId,
            ProductId = product.Id,
            Product = product,
            Status = status,
            CurrentPrice = currentPrice,
            StartingPrice = currentPrice,
            BidStep = bidStep,
            RequiresRegistration = true,
            RegistrationStartDate = UtcNow.AddDays(-7),
            RegistrationEndDate = UtcNow.AddHours(-2),
            StartDate = startDate ?? UtcNow.AddHours(-1),
            EndDate = endDate ?? UtcNow.AddHours(1)
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auctionId;
    }

    private static ApplicationUser CreateUser(int id, string email) =>
        new()
        {
            Id = id,
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            PhoneNumber = "0000000000",
            FullName = email,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString()
        };

    private sealed class StubAuctionRegistrationService(string? blockMessage) : IAuctionRegistrationService
    {
        public Task<string?> GetBidBlockMessageAsync(int auctionId, int userId, bool requiresRegistration)
        {
            if (!requiresRegistration || blockMessage is null)
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(blockMessage);
        }

        public Task<AuctionRegistrationResult> RegisterAsync(int auctionId, int userId) =>
            throw new NotImplementedException();

        public Task<AuctionRegistrationResult> CancelRegistrationAsync(int auctionId, int userId) =>
            throw new NotImplementedException();
    }

    private sealed class NoOpBidFraudDetectionService : IBidFraudDetectionService
    {
        public Task EvaluateAsync(
            int auctionId,
            long bidId,
            int bidderId,
            decimal previousPrice,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpRabbitMqPublisher : IRabbitMqPublisher
    {
        public bool IsEnabled => false;

        public bool TryPublish<T>(string routingKey, T message) => true;
    }

    private sealed class NoOpServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new NotImplementedException();
    }

    private sealed class NoOpHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
