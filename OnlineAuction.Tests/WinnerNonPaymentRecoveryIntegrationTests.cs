using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class WinnerNonPaymentRecoveryIntegrationTests
{
    [Fact]
    public async Task CancelAllExpiredPendingOrders_OffersSecondChanceAndForfeitsDeposit()
    {
        var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDbContext();
        var auctionId = await SeedSecondChanceScenarioAsync(db, now);

        var orderService = CreateOrderService(db);
        var cancelledCount = await orderService.CancelAllExpiredPendingOrdersAsync();

        Assert.Equal(1, cancelledCount);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal(AuctionStatuses.AwaitingPayment, auction!.Status);
        Assert.Equal(2, auction.WinnerId);

        var forfeitedDeposit = await db.AuctionRegistrationDeposits.SingleAsync();
        Assert.Equal(AuctionRegistrationDepositStatuses.Forfeited, forfeitedDeposit.Status);
        Assert.NotNull(forfeitedDeposit.ForfeitedAt);

        var orders = await db.Orders.Include(order => order.Items).ToListAsync();
        Assert.Equal(2, orders.Count);

        var cancelledOrder = orders.Single(order => order.Status == OrderStatuses.Cancelled);
        Assert.Equal(1, cancelledOrder.BuyerId);

        var secondChanceOrder = orders.Single(order => order.Status == OrderStatuses.PendingPayment);
        Assert.Equal(2, secondChanceOrder.BuyerId);
        Assert.Contains("-SC2", secondChanceOrder.OrderReference);

        var winningBid = await db.Bids.SingleAsync(bid => bid.IsWinning);
        Assert.Equal(2, winningBid.BidderId);

        var logs = await db.WinnerNonPaymentLogs.OrderBy(log => log.Id).ToListAsync();
        Assert.Contains(logs, log => log.Action == WinnerNonPaymentActions.PaymentExpired);
        Assert.Contains(logs, log => log.Action == WinnerNonPaymentActions.DepositForfeited);
        Assert.Contains(logs, log => log.Action == WinnerNonPaymentActions.SecondChanceOffered);
    }

    [Fact]
    public async Task CancelAllExpiredPendingOrders_ClosesAuctionWhenNoRunnerUp()
    {
        var now = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDbContext();
        var auctionId = await SeedRelistScenarioAsync(db, now);

        var orderService = CreateOrderService(db);
        var cancelledCount = await orderService.CancelAllExpiredPendingOrdersAsync();

        Assert.Equal(1, cancelledCount);

        var auction = await db.Auctions.FindAsync(auctionId);
        Assert.NotNull(auction);
        Assert.Equal(AuctionStatuses.Ended, auction!.Status);
        Assert.Null(auction.WinnerId);

        Assert.False(await db.Bids.AnyAsync(bid => bid.IsWinning));
        Assert.Single(await db.Orders.ToListAsync());
        Assert.Contains(
            await db.WinnerNonPaymentLogs.Select(log => log.Action).ToListAsync(),
            action => action == WinnerNonPaymentActions.RelistRecommended);
    }

    private static AuctionHouseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuctionHouseDbContext(options);
    }

    private static OrderService CreateOrderService(AuctionHouseDbContext db)
    {
        var feeSettings = Options.Create(new PlatformFeeSettings());
        var winnerSettings = Options.Create(new WinnerNonPaymentSettings { SecondChancePaymentHours = 48 });

        var notificationService = new NoOpNotificationService();
        var realtimePublisher = new NoOpRealtimePublisher();
        var bidService = new NoOpBidService();
        var depositRefundService = new NoOpRegistrationDepositRefundService();

        var orderCreationService = new OrderCreationService(
            db,
            NullLogger<OrderCreationService>.Instance,
            notificationService,
            depositRefundService,
            realtimePublisher,
            bidService,
            feeSettings);

        var recoveryService = new WinnerNonPaymentRecoveryService(
            db,
            orderCreationService,
            notificationService,
            realtimePublisher,
            bidService,
            winnerSettings,
            NullLogger<WinnerNonPaymentRecoveryService>.Instance);

        return new OrderService(db, feeSettings, recoveryService);
    }

    private static async Task<int> SeedSecondChanceScenarioAsync(AuctionHouseDbContext db, DateTime now)
    {
        const int auctionId = 100;
        const int sellerId = 10;
        const int winnerId = 1;
        const int runnerUpId = 2;

        var product = new Product
        {
            Id = 1,
            SellerId = sellerId,
            CategoryId = 1,
            Name = "Test Card",
            PrimaryImage = "/img.png",
            GradeLabel = "PSA 10"
        };

        var auction = new Auction
        {
            Id = auctionId,
            ProductId = product.Id,
            Product = product,
            Status = AuctionStatuses.AwaitingPayment,
            WinnerId = winnerId,
            StartingPrice = 50m,
            BidStep = 5m,
            CurrentPrice = 100m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddDays(-5),
            StartDate = now.AddDays(-3),
            EndDate = now.AddDays(-1)
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        db.Bids.AddRange(
            new Bid
            {
                Id = 1,
                AuctionId = auctionId,
                BidderId = winnerId,
                Amount = 100m,
                IsWinning = true,
                PlacedAt = now.AddHours(-2)
            },
            new Bid
            {
                Id = 2,
                AuctionId = auctionId,
                BidderId = runnerUpId,
                Amount = 90m,
                IsWinning = false,
                PlacedAt = now.AddHours(-3)
            });

        db.AuctionRegistrationDeposits.Add(new AuctionRegistrationDeposit
        {
            Id = 1,
            AuctionId = auctionId,
            UserId = winnerId,
            AuctionRegistrationId = 1,
            Amount = 50m,
            Status = AuctionRegistrationDepositStatuses.Paid,
            PaidAt = now.AddDays(-2)
        });

        var expiredOrder = new AuctionOrder
        {
            Id = 1,
            OrderReference = "AH-20260715-100",
            BuyerId = winnerId,
            Subtotal = 100m,
            ShippingFee = 45m,
            VaultInsurance = 60m,
            PlatformFee = 2.5m,
            DepositApplied = 50m,
            TotalAmount = 157.5m,
            Status = OrderStatuses.PendingPayment,
            OrderSource = OrderSources.AuctionWin,
            PaymentDeadline = now.AddHours(-1),
            CreatedAt = now.AddDays(-1),
            Items =
            [
                new OrderItem
                {
                    AuctionId = auctionId,
                    ItemName = product.Name,
                    ItemGrade = product.GradeLabel,
                    ItemImageUrl = product.PrimaryImage,
                    WinningBid = 100m,
                    CreatedAt = now.AddDays(-1)
                }
            ]
        };

        db.Orders.Add(expiredOrder);
        await db.SaveChangesAsync();
        return auctionId;
    }

    private static async Task<int> SeedRelistScenarioAsync(AuctionHouseDbContext db, DateTime now)
    {
        const int auctionId = 200;
        const int sellerId = 10;
        const int winnerId = 1;

        var product = new Product
        {
            Id = 2,
            SellerId = sellerId,
            CategoryId = 1,
            Name = "Solo Bid Card",
            PrimaryImage = "/img2.png",
            GradeLabel = "PSA 9"
        };

        var auction = new Auction
        {
            Id = auctionId,
            ProductId = product.Id,
            Product = product,
            Status = AuctionStatuses.AwaitingPayment,
            WinnerId = winnerId,
            StartingPrice = 40m,
            BidStep = 5m,
            CurrentPrice = 80m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddDays(-5),
            StartDate = now.AddDays(-3),
            EndDate = now.AddDays(-1)
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        db.Bids.Add(new Bid
        {
            Id = 3,
            AuctionId = auctionId,
            BidderId = winnerId,
            Amount = 80m,
            IsWinning = true,
            PlacedAt = now.AddHours(-2)
        });

        db.Orders.Add(new AuctionOrder
        {
            Id = 2,
            OrderReference = "AH-20260715-200",
            BuyerId = winnerId,
            Subtotal = 80m,
            ShippingFee = 45m,
            VaultInsurance = 60m,
            PlatformFee = 2m,
            TotalAmount = 187m,
            Status = OrderStatuses.PendingPayment,
            OrderSource = OrderSources.AuctionWin,
            PaymentDeadline = now.AddHours(-1),
            CreatedAt = now.AddDays(-1),
            Items =
            [
                new OrderItem
                {
                    AuctionId = auctionId,
                    ItemName = product.Name,
                    ItemGrade = product.GradeLabel,
                    ItemImageUrl = product.PrimaryImage,
                    WinningBid = 80m,
                    CreatedAt = now.AddDays(-1)
                }
            ]
        });

        await db.SaveChangesAsync();
        return auctionId;
    }

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
            CancellationToken cancellationToken = default)
            => Task.FromResult<NotificationItemViewModel?>(null);

        public Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
            int userId,
            int limit = 20,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<NotificationItemViewModel>>([]);

        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RegisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            string? deviceInfo,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UnregisterDeviceTokenAsync(
            int userId,
            string fcmToken,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ProcessAuctionEndingSoonNotificationsAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ProcessAuctionStartingSoonNotificationsAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpRealtimePublisher : IRealtimePublisher
    {
        public Task SendNotificationToUserAsync(
            int userId,
            NotificationItemViewModel notification,
            int unreadCount,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendOrderCountToUserAsync(
            int userId,
            int orderCount,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendBidUpdateAsync(
            int auctionId,
            AuctionBidStateViewModel state,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpBidService : IBidService
    {
        public Task<PlaceBidResult> PlaceBidAsync(int auctionId, int bidderId, decimal amount)
            => Task.FromResult(new PlaceBidResult { Success = false, Message = "Not used in tests." });

        public Task<AuctionBidStateViewModel?> GetBidStateAsync(
            int auctionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<AuctionBidStateViewModel?>(null);
    }

    private sealed class NoOpRegistrationDepositRefundService : IRegistrationDepositRefundService
    {
        public Task<RegistrationDepositResult> RefundDepositAsync(
            long depositId,
            bool pushNotification = true,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RegistrationDepositResult { Success = true });

        public Task<int> RefundLoserDepositsForAuctionAsync(
            int auctionId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
