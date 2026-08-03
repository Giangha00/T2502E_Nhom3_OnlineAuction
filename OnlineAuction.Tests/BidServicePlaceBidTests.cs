using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Messaging;
using OnlineAuction.Messaging.Handlers;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class BidServicePlaceBidTests
{
    [Fact]
    public async Task PlaceBidAsync_InvalidAuctionId_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId: 0, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("Invalid bid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_AuctionNotFound_Returns404()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 2);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId: 999, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task PlaceBidAsync_InactiveBidder_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 2, status: UserStatus.Inactive);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("not active", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_OwnListing_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 1, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("own listing", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_BelowMinimum_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 105m);

        Assert.False(result.Success);
        Assert.Contains("at least", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_InvalidIncrement_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 115m);

        Assert.False(result.Success);
        Assert.Contains("per step", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_RegistrationBlocked_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, requiresRegistration: true);
        var service = CreateBidService(
            db,
            registration: new StubRegistrationService("Your registration is pending approval."));

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("registration", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_LiveAuction_Succeeds()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, currentPrice: 100m, bidStep: 10m);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.True(result.Success);
        Assert.Equal(110m, result.CurrentPrice);
        Assert.Equal(1, result.BidCount);
        Assert.Equal(120m, result.MinNextBid);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.Equal(110m, auction!.CurrentPrice);
        Assert.Single(db.Bids.Where(b => b.AuctionId == auctionId));
    }

    [Fact]
    public async Task PlaceBidAsync_ScheduledBeforeLive_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(
            db,
            sellerId: 1,
            status: AuctionStatuses.Scheduled,
            startDate: DateTime.UtcNow.AddDays(1),
            endDate: DateTime.UtcNow.AddDays(2));
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("not started", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AuctionStatuses.Confirming, "confirming")]
    [InlineData(AuctionStatuses.AwaitingPayment, "ended")]
    [InlineData(AuctionStatuses.Completed, "completed")]
    public async Task PlaceBidAsync_NonLiveStatus_ReturnsFailure(string status, string expectedFragment)
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, status: status);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains(expectedFragment, result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceBidAsync_EndedAuction_ReturnsFailure()
    {
        await using var db = await CreateContextAsync();
        await SeedUserAsync(db, id: 1);
        await SeedUserAsync(db, id: 2);
        var auctionId = await SeedLiveAuctionAsync(db, sellerId: 1, status: AuctionStatuses.Ended);
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: 110m);

        Assert.False(result.Success);
        Assert.Contains("ended", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 110)]
    [InlineData(-1, 110)]
    [InlineData(1, 0)]
    public async Task PlaceBidAsync_InvalidAmounts_ReturnFailure(int auctionId, decimal amount)
    {
        await using var db = await CreateContextAsync();
        var service = CreateBidService(db);

        var result = await service.PlaceBidAsync(auctionId, bidderId: 2, amount: amount);

        Assert.False(result.Success);
    }

    private static BidService CreateBidService(
        AuctionHouseDbContext db,
        StubRegistrationService? registration = null)
    {
        registration ??= new StubRegistrationService();

        var services = new ServiceCollection();
        services.AddSingleton<IBidPlacedMessageHandler, NoOpBidPlacedHandler>();
        var provider = services.BuildServiceProvider();

        return new BidService(
            db,
            registration,
            new AllowAllFraudDetection(),
            new NoOpRabbitMqPublisher(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            new HttpContextAccessor(),
            Options.Create(new BidFraudDetectionSettings()),
            NullLogger<BidService>.Instance);
    }

    private static async Task<AuctionHouseDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            await pragma.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AuctionHouseDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task SeedUserAsync(
        AuctionHouseDbContext db,
        int id,
        UserStatus status = UserStatus.Active)
    {
        if (await db.Users.AnyAsync(u => u.Id == id))
        {
            return;
        }

        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = $"user{id}",
            Email = $"user{id}@test.local",
            FullName = $"User {id}",
            PhoneNumber = $"090000{id:0000}",
            Status = status,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedLiveAuctionAsync(
        AuctionHouseDbContext db,
        int sellerId,
        decimal currentPrice = 100m,
        decimal bidStep = 10m,
        string status = AuctionStatuses.Live,
        bool requiresRegistration = false,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == 1))
        {
            db.Categories.Add(new Category
            {
                Id = 1,
                Name = "Pokemon",
                Slug = "pokemon",
                CreatedAt = DateTime.UtcNow
            });
        }

        var now = DateTime.UtcNow;
        var registrationStart = now.AddDays(-7);
        var registrationEnd = now.AddHours(-1);
        var liveStart = startDate ?? now.AddHours(-1);
        var liveEnd = endDate ?? now.AddDays(1);

        var product = new Product
        {
            SellerId = sellerId,
            CategoryId = 1,
            Name = $"Card {Guid.NewGuid():N}"[..16],
            PrimaryImage = "https://example.com/card.jpg",
            ShortDescription = "Test card",
            CreatedAt = now
        };

        var auction = new Auction
        {
            Product = product,
            Status = status,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = requiresRegistration,
            StartingPrice = currentPrice,
            BidStep = bidStep,
            CurrentPrice = currentPrice,
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            SubmittedAt = now,
            CreatedAt = now
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction.Id;
    }

    private sealed class StubRegistrationService(string? blockMessage = null) : IAuctionRegistrationService
    {
        public Task<string?> GetBidBlockMessageAsync(int auctionId, int userId, bool requiresRegistration) =>
            Task.FromResult(blockMessage);

        public Task<AuctionRegistrationResult> RegisterAsync(int auctionId, int userId) =>
            throw new NotImplementedException();

        public Task<AuctionRegistrationResult> CancelRegistrationAsync(int auctionId, int userId) =>
            throw new NotImplementedException();
    }

    private sealed class AllowAllFraudDetection : IBidFraudDetectionService
    {
        public Task<BidFraudGateResult> EvaluatePreBidAsync(
            int auctionId,
            int bidderId,
            decimal amount,
            decimal previousPrice,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BidFraudGateResult(true));

        public Task EvaluatePostBidAsync(
            int auctionId,
            long bidId,
            int bidderId,
            decimal previousPrice,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpRabbitMqPublisher : IRabbitMqPublisher
    {
        public bool IsEnabled => false;

        public bool TryPublish<T>(string queueName, T message) => false;
    }

    private sealed class NoOpBidPlacedHandler : IBidPlacedMessageHandler
    {
        public Task HandleAsync(BidPlacedMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
