using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Models.PayPal;
using OnlineAuction.Services;
using OnlineAuction.Services.Interfaces;
using Xunit;

namespace OnlineAuction.Tests;

public class PayPalCaptureFlowTests
{
    private const string PayPalOrderId = "PAYPAL-ORDER-1";
    private const string CaptureId = "CAPTURE-1";

    [Fact]
    public async Task SafeCapture_RejectsAmountMismatchBeforeCapture()
    {
        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 50m)
        };

        var guard = CreateGuard(CreateDbContext(), payPal);
        var result = await guard.SafeCaptureAsync(
            PayPalOrderId,
            100m,
            new PayPalCaptureContext("order", 1, OrderId: 10),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, payPal.CaptureCallCount);
        Assert.Equal(0, payPal.RefundCallCount);
    }

    [Fact]
    public async Task SafeCapture_RefundsWhenPostCaptureAmountDiffers()
    {
        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 50m)
        };

        var guard = CreateGuard(CreateDbContext(), payPal);
        var result = await guard.SafeCaptureAsync(
            PayPalOrderId,
            100m,
            new PayPalCaptureContext("order", 1, OrderId: 10),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.RefundAttempted);
        Assert.True(result.RefundSucceeded);
        Assert.Equal(1, payPal.CaptureCallCount);
        Assert.Equal(1, payPal.RefundCallCount);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_RejectsCancelledOrderBeforeCapture()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.Cancelled);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m)
        };

        var service = CreateOrderPaymentService(db, payPal);
        var result = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.False(result.Success);
        Assert.Equal(0, payPal.CaptureCallCount);
        Assert.Equal(OrderStatuses.Cancelled, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_HappyPathMarksOrderPaid()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.PendingPayment);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 100m)
        };

        var service = CreateOrderPaymentService(db, payPal);
        var result = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.True(result.Success);
        Assert.Equal(OrderStatuses.Paid, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(PaymentStatuses.Success, (await db.Payments.SingleAsync()).Status);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_RejectsWhenSandboxWalletInsufficient()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.PendingPayment);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 100m)
        };

        var service = CreateOrderPaymentService(db, payPal, sandboxInitialBalance: 50m, enforceSandboxWallet: true);
        var result = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.False(result.Success);
        Assert.Contains("nhỏ hơn", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, payPal.CaptureCallCount);
        Assert.Equal(OrderStatuses.PendingPayment, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_AllowsPaymentWhenSandboxWalletEnforcementDisabled()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.PendingPayment);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 100m)
        };

        // Default: EnforceSandboxWallet=false — low local ledger must not block PayPal capture.
        var service = CreateOrderPaymentService(db, payPal, sandboxInitialBalance: 1m);
        var result = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.True(result.Success);
        Assert.Equal(1, payPal.CaptureCallCount);
        Assert.Equal(OrderStatuses.Paid, (await db.Orders.SingleAsync()).Status);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_DeductsSandboxWalletOnSuccess()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.PendingPayment);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 100m)
        };

        var service = CreateOrderPaymentService(db, payPal, sandboxInitialBalance: 250m, enforceSandboxWallet: true);
        var result = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.True(result.Success);
        var wallet = await db.UserSandboxWallets.SingleAsync(item => item.UserId == 1);
        Assert.Equal(150m, wallet.Balance);
    }

    [Fact]
    public async Task CapturePayPalCheckoutAsync_DoubleReturnUrl_IsIdempotent()
    {
        await using var db = CreateDbContext();
        await SeedOrderPaymentAsync(db, OrderStatuses.PendingPayment);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 100m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 100m)
        };

        var service = CreateOrderPaymentService(db, payPal);
        var first = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);
        var second = await service.CapturePayPalCheckoutAsync(1, PayPalOrderId);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, payPal.CaptureCallCount);
    }

    [Fact]
    public async Task CaptureDepositAsync_RejectsNonPendingDepositBeforeCapture()
    {
        await using var db = CreateDbContext();
        await SeedDepositAsync(db, AuctionRegistrationDepositStatuses.Cancelled);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 25m)
        };

        var service = CreateRegistrationDepositService(db, payPal);
        var result = await service.CaptureDepositAsync(1, PayPalOrderId);

        Assert.False(result.Success);
        Assert.Equal(0, payPal.CaptureCallCount);
    }

    [Fact]
    public async Task CaptureDepositAsync_HappyPathMarksDepositPaid()
    {
        await using var db = CreateDbContext();
        await SeedDepositAsync(db, AuctionRegistrationDepositStatuses.Pending);

        var payPal = new FakePayPalService
        {
            OrderDetails = PayPalOrderDetailsResult.Ok(PayPalOrderId, "APPROVED", 25m),
            CaptureResult = PayPalCaptureResult.Ok(CaptureId, 25m)
        };

        var service = CreateRegistrationDepositService(db, payPal);
        var result = await service.CaptureDepositAsync(1, PayPalOrderId);

        Assert.True(result.Success);
        var deposit = await db.AuctionRegistrationDeposits.SingleAsync();
        Assert.Equal(AuctionRegistrationDepositStatuses.Paid, deposit.Status);
        Assert.Equal(CaptureId, deposit.PayPalCaptureId);
    }

    [Fact]
    public void AmountsMatch_UsesOneCentTolerance()
    {
        Assert.True(PayPalAmountHelper.AmountsMatch(100m, 100.009m));
        Assert.False(PayPalAmountHelper.AmountsMatch(100m, 100.01m));
    }

    private static PayPalCaptureGuardService CreateGuard(AuctionHouseDbContext db, FakePayPalService payPal) =>
        new(
            payPal,
            db,
            new NoOpNotificationService(),
            new NoOpNotificationLocalizer(),
            NullLogger<PayPalCaptureGuardService>.Instance);

    private static OrderPaymentService CreateOrderPaymentService(
        AuctionHouseDbContext db,
        FakePayPalService payPal,
        decimal sandboxInitialBalance = 10_000m,
        bool enforceSandboxWallet = false)
    {
        var guard = CreateGuard(db, payPal);
        var wallet = new SandboxPayPalWalletService(
            db,
            Options.Create(new PayPalSettings
            {
                Mode = "sandbox",
                SandboxInitialWalletBalance = sandboxInitialBalance,
                EnforceSandboxWallet = enforceSandboxWallet,
                CurrencyCode = "USD"
            }));

        return new OrderPaymentService(
            db,
            payPal,
            guard,
            wallet,
            new NoOpNotificationService(),
            new NoOpNotificationLocalizer(),
            new NoOpOrderService(),
            new NoOpRealtimePublisher(),
            NullLogger<OrderPaymentService>.Instance,
            Options.Create(new PlatformFeeSettings()));
    }

    private static RegistrationDepositService CreateRegistrationDepositService(
        AuctionHouseDbContext db,
        FakePayPalService payPal)
    {
        var guard = CreateGuard(db, payPal);
        return new RegistrationDepositService(
            db,
            payPal,
            guard,
            new NoOpNotificationService(),
            new NoOpNotificationLocalizer(),
            NullLogger<RegistrationDepositService>.Instance,
            Options.Create(new PlatformFeeSettings()));
    }

    private static AuctionHouseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuctionHouseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AuctionHouseDbContext(options);
    }

    private static async Task SeedOrderPaymentAsync(AuctionHouseDbContext db, string orderStatus)
    {
        var now = DateTime.UtcNow;
        db.Users.Add(new ApplicationUser
        {
            Id = 1,
            UserName = "buyer",
            NormalizedUserName = "BUYER",
            Email = "buyer@test.com",
            NormalizedEmail = "BUYER@TEST.COM",
            FullName = "Buyer",
            PhoneNumber = "000",
            CreatedAt = now
        });

        var product = new Product
        {
            Id = 1,
            SellerId = 2,
            CategoryId = 1,
            Name = "Card",
            PrimaryImage = "/img.png"
        };

        var auction = new Auction
        {
            Id = 10,
            ProductId = 1,
            Product = product,
            Status = AuctionStatuses.AwaitingPayment,
            StartingPrice = 50m,
            BidStep = 5m,
            CurrentPrice = 100m,
            RegistrationStartDate = now.AddDays(-7),
            RegistrationEndDate = now.AddDays(-5),
            StartDate = now.AddDays(-3),
            EndDate = now.AddDays(-1)
        };

        var order = new AuctionOrder
        {
            Id = 100,
            BuyerId = 1,
            OrderReference = "ORD-1",
            Subtotal = 100m,
            TotalAmount = 100m,
            Status = orderStatus,
            PaymentDeadline = now.AddDays(1),
            ShippingAddress = "123 Test Street",
            CreatedAt = now,
            Items =
            [
                new OrderItem
                {
                    AuctionId = 10,
                    ItemName = "Card",
                    WinningBid = 100m
                }
            ]
        };

        db.Products.Add(product);
        db.Auctions.Add(auction);
        db.Orders.Add(order);
        db.Payments.Add(new Payment
        {
            OrderId = 100,
            Amount = 100m,
            Status = PaymentStatuses.Pending,
            PayPalOrderId = PayPalOrderId,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedDepositAsync(AuctionHouseDbContext db, string depositStatus)
    {
        var now = DateTime.UtcNow;
        db.Users.Add(new ApplicationUser
        {
            Id = 1,
            UserName = "bidder",
            NormalizedUserName = "BIDDER",
            Email = "bidder@test.com",
            NormalizedEmail = "BIDDER@TEST.COM",
            FullName = "Bidder",
            PhoneNumber = "001",
            CreatedAt = now
        });

        var registration = new AuctionRegistration
        {
            Id = 1,
            AuctionId = 10,
            UserId = 1,
            Status = AuctionRegistrationStatuses.Pending,
            RegisteredAt = now,
            CreatedAt = now
        };

        db.AuctionRegistrations.Add(registration);
        db.AuctionRegistrationDeposits.Add(new AuctionRegistrationDeposit
        {
            AuctionId = 10,
            UserId = 1,
            AuctionRegistrationId = 1,
            Amount = 25m,
            Status = depositStatus,
            PayPalOrderId = PayPalOrderId,
            Registration = registration,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
    }

    private sealed class FakePayPalService : IPayPalService
    {
        public int CaptureCallCount { get; private set; }

        public int RefundCallCount { get; private set; }

        public PayPalOrderDetailsResult OrderDetails { get; set; } =
            PayPalOrderDetailsResult.Fail("not configured");

        public PayPalCaptureResult CaptureResult { get; set; } =
            PayPalCaptureResult.Fail("not configured");

        public PayPalRefundResult RefundResult { get; set; } =
            PayPalRefundResult.Ok("REFUND-1", "COMPLETED");

        public Task<PayPalCreateOrderResult> CreateCheckoutOrderAsync(
            decimal totalAmount,
            string referenceId,
            string returnUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PayPalCreateOrderResult.Ok(PayPalOrderId, "https://paypal.test/approve"));

        public Task<PayPalOrderDetailsResult> GetOrderDetailsAsync(
            string payPalOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OrderDetails);

        public Task<PayPalCaptureResult> CaptureOrderAsync(
            string payPalOrderId,
            CancellationToken cancellationToken = default)
        {
            CaptureCallCount++;
            return Task.FromResult(CaptureResult);
        }

        public Task<PayPalCancelResult> CancelOrderAsync(
            string payPalOrderId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PayPalCancelResult.Ok());

        public Task<PayPalVerifyWebhookResult> VerifyWebhookSignatureAsync(
            string requestBody,
            IHeaderDictionary headers,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PayPalVerifyWebhookResult.Ok());

        public Task<PayPalRefundResult> RefundCaptureAsync(
            string captureId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            RefundCallCount++;
            return Task.FromResult(RefundResult);
        }
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<NotificationItemViewModel?>(null);

        public Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
            int userId,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationItemViewModel>>([]);

        public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> MarkAsReadAsync(
            int userId,
            int notificationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

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
            string.Format(System.Globalization.CultureInfo.InvariantCulture, name, args);
    }

    private sealed class NoOpOrderService : IOrderService
    {
        public Task<OrderPageViewModel?> BuildOrderPageAsync(int buyerId) =>
            Task.FromResult<OrderPageViewModel?>(null);

        public Task<int> CountPendingPaymentOrdersAsync(int buyerId) =>
            Task.FromResult(0);

        public Task<int> CancelExpiredPendingOrdersAsync(int buyerId) =>
            Task.FromResult(0);

        public Task<int> CancelAllExpiredPendingOrdersAsync() =>
            Task.FromResult(0);

        public Task<(bool Success, string Message)> CompleteOrderAsync(
            int buyerId,
            CompleteOrderRequest request) =>
            Task.FromResult((false, "not implemented"));

        public Task<(bool Success, string Message, int ClearedCount)> ClearAllBuyNowOrdersAsync(
            int buyerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((false, "not implemented", 0));
    }

    private sealed class NoOpRealtimePublisher : IRealtimePublisher
    {
        public Task SendNotificationToUserAsync(
            int userId,
            NotificationItemViewModel notification,
            int unreadCount,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendOrderCountToUserAsync(
            int userId,
            int orderCount,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendBidUpdateAsync(
            int auctionId,
            AuctionBidStateViewModel state,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
