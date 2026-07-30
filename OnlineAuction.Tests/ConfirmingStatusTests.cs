using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Areas.Admin.Services;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class ConfirmingStatusTests
{
    [Fact]
    public void AuctionStatuses_Confirming_IsCanonicalAndAliasesPendingReview()
    {
        Assert.Equal("confirming", AuctionStatuses.Confirming);
        Assert.Equal(AuctionStatuses.Confirming, AuctionStatuses.PendingReview);
        Assert.Contains(AuctionStatuses.Confirming, AuctionStatuses.All);
        Assert.DoesNotContain("pending_review", AuctionStatuses.All);
    }

    [Fact]
    public void IsPubliclyListed_DeniesConfirmingAndRejected()
    {
        var now = DateTime.UtcNow;
        var confirming = CreateAuction(AuctionStatuses.Confirming, now);
        var rejected = CreateAuction(AuctionStatuses.Rejected, now);
        var live = CreateAuction(AuctionStatuses.Live, now);

        Assert.False(AuctionScheduleHelper.IsPubliclyListed(confirming, now));
        Assert.False(AuctionScheduleHelper.IsPubliclyListed(rejected, now));
        Assert.True(AuctionScheduleHelper.IsPubliclyListed(live, now));
    }

    [Fact]
    public void ProductDocumentAccessPolicy_Confirming_IsNotPublic()
    {
        Assert.False(ProductDocumentAccessPolicy.IsPublicAuctionStatus(AuctionStatuses.Confirming));
        Assert.False(ProductDocumentAccessPolicy.CanAnonymousDownload([AuctionStatuses.Confirming]));
    }

    [Fact]
    public async Task GetPendingCount_CountsConfirmingOnly()
    {
        await using var db = await CreateContextAsync();
        await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1);
        await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1);
        await SeedAuctionAsync(db, AuctionStatuses.Live, sellerId: 1);
        await SeedAuctionAsync(db, AuctionStatuses.Rejected, sellerId: 1);

        var service = new AdminAuctionVerificationService(
            db,
            new NoOpNotificationService(),
            new NoOpNotificationLocalizer(),
            NullLogger<AdminAuctionVerificationService>.Instance);

        var count = await service.GetPendingCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetAuctionIndex_ExcludesConfirmingListings()
    {
        await using var db = await CreateContextAsync();
        var confirmingId = await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1);
        var liveId = await SeedAuctionAsync(db, AuctionStatuses.Live, sellerId: 1);

        var service = new AuctionService(db, Options.Create(new PlatformFeeSettings()));
        var index = await service.GetAuctionIndexAsync();

        Assert.DoesNotContain(index.Auctions, item => item.Id == confirmingId);
        Assert.Contains(index.Auctions, item => item.Id == liveId);
    }

    [Fact]
    public async Task GetHomePage_ExcludesConfirmingListings()
    {
        await using var db = await CreateContextAsync();
        var confirmingId = await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1);
        var liveId = await SeedAuctionAsync(db, AuctionStatuses.Live, sellerId: 1);

        var service = new AuctionService(db, Options.Create(new PlatformFeeSettings()));
        var home = await service.GetHomePageAsync();

        Assert.DoesNotContain(home.TrendingOnAuction, item => item.Id == confirmingId);
        Assert.Contains(home.TrendingOnAuction, item => item.Id == liveId);
    }

    [Fact]
    public async Task GetProductDetail_Confirming_HiddenFromNonOwner()
    {
        await using var db = await CreateContextAsync();
        var auctionId = await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 10);

        var service = new AuctionService(db, Options.Create(new PlatformFeeSettings()));

        Assert.Null(await service.GetProductDetailAsync(auctionId, currentUserId: 99));
        Assert.Null(await service.GetProductDetailAsync(auctionId, currentUserId: null));

        var ownerView = await service.GetProductDetailAsync(auctionId, currentUserId: 10);
        Assert.NotNull(ownerView);
        Assert.Equal("Confirming", ownerView!.AuctionStatus);

        var adminView = await service.GetProductDetailAsync(auctionId, currentUserId: 99, isAdmin: true);
        Assert.NotNull(adminView);
    }

    [Fact]
    public async Task Register_Confirming_IsBlocked()
    {
        await using var db = await CreateContextAsync();
        var auctionId = await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1, requiresRegistration: true);
        db.Users.Add(CreateUser(2, "Buyer"));
        await db.SaveChangesAsync();

        var registrationService = new AuctionRegistrationService(
            db,
            new NoOpDepositRefundService(),
            new NoOpNotificationService(),
            new NoOpNotificationLocalizer(),
            NullLogger<AuctionRegistrationService>.Instance);

        var result = await registrationService.RegisterAsync(auctionId, userId: 2);

        Assert.False(result.Success);
        Assert.Contains("confirming", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Watchlist_Toggle_CannotAddOthersConfirmingListing()
    {
        await using var db = await CreateContextAsync();
        var auctionId = await SeedAuctionAsync(db, AuctionStatuses.Confirming, sellerId: 1);
        db.Users.Add(CreateUser(2, "Watcher"));
        await db.SaveChangesAsync();

        var watchlist = new WatchlistService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => watchlist.ToggleAsync(userId: 2, auctionId));

        Assert.Contains("not available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDownloadAsync_Confirming_DeniesAnonymous_AllowsAdmin()
    {
        await using var db = await CreateContextAsync();
        var documentId = await SeedDocumentAsync(db, AuctionStatuses.Confirming);
        var service = new ProductDocumentDownloadService(db);

        var anon = await service.GetDownloadAsync(documentId, isAdminRequest: false);
        Assert.Equal(ProductDocumentDownloadStatus.Forbidden, anon!.Status);

        var admin = await service.GetDownloadAsync(documentId, isAdminRequest: true);
        Assert.Equal(ProductDocumentDownloadStatus.Success, admin!.Status);
    }

    private static Auction CreateAuction(string status, DateTime now) =>
        new()
        {
            Status = status,
            StartingPrice = 100m,
            BidStep = 10m,
            CurrentPrice = 100m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddHours(-1),
            StartDate = now.AddHours(-1),
            EndDate = now.AddHours(2),
            CreatedAt = now
        };

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

    private static async Task<int> SeedAuctionAsync(
        AuctionHouseDbContext db,
        string status,
        int sellerId,
        bool requiresRegistration = false)
    {
        if (!await db.Users.AnyAsync(u => u.Id == sellerId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = sellerId,
                UserName = $"seller{sellerId}",
                Email = $"seller{sellerId}@test.local",
                FullName = $"Seller {sellerId}",
                PhoneNumber = $"090000{sellerId:0000}",
                CreatedAt = DateTime.UtcNow
            });
        }

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
        var product = new Product
        {
            SellerId = sellerId,
            CategoryId = 1,
            Name = $"Card {Guid.NewGuid():N}"[..20],
            PrimaryImage = "https://example.com/image.jpg",
            ShortDescription = "Test",
            CreatedAt = now
        };

        var auction = new Auction
        {
            Product = product,
            Status = status,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = requiresRegistration,
            StartingPrice = 100m,
            BidStep = 10m,
            CurrentPrice = 100m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddHours(-1),
            StartDate = now.AddHours(-1),
            EndDate = now.AddDays(1),
            SubmittedAt = now,
            CreatedAt = now
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
        return auction.Id;
    }

    private static async Task<int> SeedDocumentAsync(AuctionHouseDbContext db, string auctionStatus)
    {
        var auctionId = await SeedAuctionAsync(db, auctionStatus, sellerId: 1);
        var auction = await db.Auctions.Include(a => a.Product).FirstAsync(a => a.Id == auctionId);

        var document = new ProductDocument
        {
            Product = auction.Product,
            Name = "PSA Certificate",
            FileUrl = "https://res.cloudinary.com/demo/image/upload/v1/auction-house/documents/cert.pdf",
            FileType = "PDF",
            CreatedAt = DateTime.UtcNow
        };

        db.ProductDocuments.Add(document);
        await db.SaveChangesAsync();
        return document.Id;
    }

    private static ApplicationUser CreateUser(int id, string name) =>
        new()
        {
            Id = id,
            UserName = name.ToLowerInvariant(),
            Email = $"{name.ToLowerInvariant()}@test.local",
            FullName = name,
            PhoneNumber = $"091000{id:0000}",
            CreatedAt = DateTime.UtcNow
        };

    private sealed class NoOpNotificationService : INotificationService
    {
        public Task<NotificationItemViewModel?> CreateAndPushAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            string? relatedUrl,
            string? referenceType = null,
            int? referenceId = null,
            TimeSpan? debounceWindow = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<NotificationItemViewModel?>(null);

        public Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
            int userId,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationItemViewModel>>([]);

        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RegisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            string? deviceInfo,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnregisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ProcessAuctionEndingSoonNotificationsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ProcessAuctionStartingSoonNotificationsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpNotificationLocalizer : INotificationLocalizer
    {
        public string this[string name] => name;

        public string Format(string name, params object[] args) =>
            OnlineAuction.Helpers.NotificationLocalization.Encode(name, args);

        public string Resolve(string? stored, string? argsJson = null) =>
            stored ?? string.Empty;
    }

    private sealed class NoOpDepositRefundService : IRegistrationDepositRefundService
    {
        public Task<RegistrationDepositResult> RefundDepositAsync(
            long depositId,
            bool pushNotification = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RegistrationDepositResult { Success = true });

        public Task<RegistrationDepositResult> RefundDepositAmountAsync(
            long depositId,
            decimal amount,
            bool pushNotification = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RegistrationDepositResult { Success = true });

        public Task<int> RefundLoserDepositsForAuctionAsync(
            int auctionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
