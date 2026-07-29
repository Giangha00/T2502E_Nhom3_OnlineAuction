using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderService : IOrderService
{
    private static readonly HashSet<string> SupportedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "paypal",
        "cod"
    };

    private readonly AuctionHouseDbContext _dbContext;
    private readonly PlatformFeeSettings _feeSettings;
    private readonly IWinnerNonPaymentRecoveryService _winnerNonPaymentRecoveryService;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly IRealtimePublisher? _realtimePublisher;

    private sealed class NullWinnerNonPaymentRecoveryService : IWinnerNonPaymentRecoveryService
    {
        public Task ProcessExpiredAuctionWinOrderAsync(
            AuctionOrder cancelledOrder,
            DateTime now,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NullNotificationService : INotificationService
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

    public OrderService(
        AuctionHouseDbContext dbContext,
        IOptions<PlatformFeeSettings> feeSettings,
        IWinnerNonPaymentRecoveryService? winnerNonPaymentRecoveryService = null,
        INotificationService? notificationService = null,
        INotificationLocalizer? notifyLocalizer = null,
        IRealtimePublisher? realtimePublisher = null)
    {
        _dbContext = dbContext;
        _feeSettings = feeSettings.Value;
        _winnerNonPaymentRecoveryService = winnerNonPaymentRecoveryService ?? new NullWinnerNonPaymentRecoveryService();
        _notificationService = notificationService ?? new NullNotificationService();
        _notifyLocalizer = notifyLocalizer ?? new NullNotificationLocalizer();
        _realtimePublisher = realtimePublisher;
    }

    public async Task<OrderPageViewModel?> BuildOrderPageAsync(int buyerId)
    {
        var buyer = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == buyerId);

        if (buyer is null)
        {
            return null;
        }

        await CancelExpiredPendingOrdersAsync(buyerId);
        await CancelInvalidPendingOrdersAsync(buyerId);

        var now = DateTime.UtcNow;
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Auction)
                    .ThenInclude(auction => auction.Product)
            .Where(BuildPayableOrdersFilter(buyerId, now))
            .OrderBy(order => order.PaymentDeadline)
            .ToListAsync();

        var items = orders
            .Select(order =>
            {
                var item = order.Items.FirstOrDefault();
                if (item is null)
                {
                    return null;
                }

                var orderSource = OrderCheckoutSelection.ResolveOrderSource(order);
                var isMandatory = orderSource == OrderSources.AuctionWin;

                return new WonOrderItem
                {
                    OrderId = order.Id,
                    AuctionId = item.AuctionId,
                    Name = item.ItemName,
                    Subtitle = BuildSubtitle(item.Auction.Product),
                    Grade = item.ItemGrade ?? string.Empty,
                    ImageUrl = item.ItemImageUrl ?? string.Empty,
                    WinningBid = item.WinningBid,
                    ShippingFee = order.ShippingFee,
                    VaultInsurance = order.VaultInsurance,
                    PlatformFee = order.PlatformFee,
                    DepositApplied = order.DepositApplied,
                    TotalAmount = order.TotalAmount,
                    PaymentDeadline = order.PaymentDeadline,
                    OrderReference = order.OrderReference,
                    OrderSource = orderSource,
                    IsMandatory = isMandatory,
                    IsSelectedByDefault = isMandatory
                };
            })
            .Where(item => item is not null)
            .Cast<WonOrderItem>()
            .ToList();

        var selectedItems = items.Where(item => item.IsSelectedByDefault).ToList();
        var orderWithShipping = orders.FirstOrDefault(order => !string.IsNullOrWhiteSpace(order.ShippingAddress));

        var model = new OrderPageViewModel
        {
            Items = items,
            FullName = orderWithShipping?.ShippingFullName ?? buyer.FullName,
            Address = orderWithShipping?.ShippingAddress ?? string.Empty,
            City = orderWithShipping?.ShippingCity ?? string.Empty,
            Phone = orderWithShipping?.ShippingPhone ?? buyer.PhoneNumber ?? string.Empty,
            SelectedPaymentMethod = orderWithShipping?.PaymentMethod,
            ShippingSaved = orderWithShipping is not null,
            SelectedItemCount = selectedItems.Count,
            PaymentMethods =
            [
                new PaymentMethodOption
                {
                    Id = "paypal",
                    Name = "PayPal",
                    Description = "Instant secure transaction"
                },
                new PaymentMethodOption
                {
                    Id = "cod",
                    Name = "Cash on Delivery (COD)",
                    Description = "Pay upon physical delivery"
                }
            ]
        };

        ApplySummary(model, selectedItems);
        return model;
    }

    public async Task<int> CountPendingPaymentOrdersAsync(int buyerId)
    {
        await CancelExpiredPendingOrdersAsync(buyerId);
        await CancelInvalidPendingOrdersAsync(buyerId);

        var now = DateTime.UtcNow;
        return await _dbContext.Orders.CountAsync(BuildPayableOrdersFilter(buyerId, now));
    }

    public async Task<int> CancelExpiredPendingOrdersAsync(int buyerId)
    {
        var now = DateTime.UtcNow;

        var expiredOrders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline <= now)
            .ToListAsync();

        return await CancelOrdersAsync(expiredOrders, now);
    }

    private async Task CancelInvalidPendingOrdersAsync(int buyerId)
    {
        var now = DateTime.UtcNow;
        var invalidOrders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                !order.Items.Any())
            .ToListAsync();

        if (invalidOrders.Count == 0)
        {
            return;
        }

        foreach (var order in invalidOrders)
        {
            order.Status = OrderStatuses.Cancelled;
            order.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<int> CancelAllExpiredPendingOrdersAsync()
    {
        var now = DateTime.UtcNow;

        var expiredOrders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline <= now)
            .ToListAsync();

        return await CancelOrdersAsync(expiredOrders, now);
    }

    public async Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        CompleteOrderRequest request)
    {
        if (!SupportedPaymentMethods.Contains(request.PaymentMethod))
        {
            return (false, "Vui lòng chọn phương thức thanh toán hợp lệ.");
        }

        var fullName = request.FullName.Trim();
        var address = request.Address.Trim();
        var city = request.City.Trim();
        var phone = request.Phone.Trim();

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(address)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(phone))
        {
            return (false, "Vui lòng điền đầy đủ thông tin giao hàng.");
        }

        await CancelExpiredPendingOrdersAsync(buyerId);

        var now = DateTime.UtcNow;
        var orders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null)
            .ToListAsync();

        var selection = OrderCheckoutSelection.Resolve(orders, request.SelectedOrderIds, now);
        if (!selection.Success)
        {
            return (false, selection.Message);
        }

        var checkoutOrders = selection.Orders;
        var paymentMethod = request.PaymentMethod.ToLowerInvariant();

        foreach (var order in checkoutOrders)
        {
            order.ShippingFullName = fullName;
            order.ShippingAddress = address;
            order.ShippingCity = city;
            order.ShippingPhone = phone;
            order.PaymentMethod = paymentMethod;
            order.UpdatedAt = now;
        }

        if (string.Equals(paymentMethod, "cod", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var order in checkoutOrders)
            {
                order.Status = OrderStatuses.Paid;
                MarketplaceFeeCalculator.ApplySellerSettlement(order, _feeSettings);

                _dbContext.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Status = PaymentStatuses.Success,
                    TransactionId = $"COD-{order.OrderReference}",
                    PaidAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await OrderCancellationHelper.MarkAuctionsCompletedAfterPaymentAsync(
                _dbContext,
                checkoutOrders,
                now);
        }

        await _dbContext.SaveChangesAsync();

        if (string.Equals(paymentMethod, "cod", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var order in checkoutOrders)
            {
                await _notificationService.CreateAndPushAsync(
                    buyerId,
                _notifyLocalizer[NotificationKeys.PaymentSuccessTitle],
                _notifyLocalizer[NotificationKeys.PaymentSuccessCodMessage],
                    NotificationType.Payment,
                    $"/Payment/Confirmation?orderId={order.Id}",
                    NotificationReferenceTypes.PaymentSuccess,
                    order.Id);

                await OrderNotificationHelper.NotifySellerPaymentReceivedAsync(
                    _notificationService,
                    _notifyLocalizer,
                    _dbContext,
                    order.Id,
                    "COD");
            }

            var references = string.Join(", ", checkoutOrders.Select(order => order.OrderReference));
            return (true, $"Thanh toán thành công! Đã xác nhận {checkoutOrders.Count} hóa đơn ({references}).");
        }

        return (true, "Đã lưu thông tin giao hàng. Bạn sẽ được chuyển đến PayPal để hoàn tất thanh toán.");
    }

    public async Task<(bool Success, string Message, int ClearedCount)> ClearAllBuyNowOrdersAsync(
        int buyerId,
        CancellationToken cancellationToken = default)
    {
        await CancelExpiredPendingOrdersAsync(buyerId);
        await CancelInvalidPendingOrdersAsync(buyerId);

        var now = DateTime.UtcNow;
        var pendingOrders = await _dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline > now &&
                order.Items.Any() &&
                (order.OrderSource == OrderSources.BuyNow
                 || order.OrderReference.StartsWith("BN-")))
            .ToListAsync(cancellationToken);

        if (pendingOrders.Count == 0)
        {
            return (true, "No Buy Now items to clear.", 0);
        }

        foreach (var order in pendingOrders)
        {
            order.Status = OrderStatuses.Cancelled;
            order.UpdatedAt = now;
            await OrderCancellationHelper.ApplyCancellationSideEffectsAsync(
                _dbContext,
                order,
                now,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_realtimePublisher is not null)
        {
            var remainingCount = await _dbContext.Orders.CountAsync(
                BuildPayableOrdersFilter(buyerId, DateTime.UtcNow),
                cancellationToken);
            await _realtimePublisher.SendOrderCountToUserAsync(buyerId, remainingCount, cancellationToken);
        }

        return (true, $"Cleared {pendingOrders.Count} Buy Now item(s).", pendingOrders.Count);
    }

    private async Task<int> CancelOrdersAsync(List<AuctionOrder> expiredOrders, DateTime now)
    {
        if (expiredOrders.Count == 0)
        {
            return 0;
        }

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatuses.Cancelled;
            order.UpdatedAt = now;

            if (OrderCheckoutSelection.ResolveOrderSource(order) == OrderSources.AuctionWin)
            {
                await _winnerNonPaymentRecoveryService.ProcessExpiredAuctionWinOrderAsync(order, now);
            }
            else
            {
                await OrderCancellationHelper.ApplyCancellationSideEffectsAsync(_dbContext, order, now);
                await OrderNotificationHelper.NotifyPaymentOverdueCancelledAsync(
                    _notificationService,
                    _notifyLocalizer,
                    _dbContext,
                    order);
            }
        }

        await _dbContext.SaveChangesAsync();
        return expiredOrders.Count;
    }

    private sealed class NullNotificationLocalizer : INotificationLocalizer
    {
        public string this[string name] => name;
        public string Format(string name, params object[] args) => string.Format(name, args);
    }

    private static void ApplySummary(OrderPageViewModel model, IReadOnlyList<WonOrderItem> selectedItems)
    {
        model.Subtotal = selectedItems.Sum(item => item.WinningBid);
        model.ShippingFee = selectedItems.Sum(item => item.ShippingFee);
        model.VaultInsurance = selectedItems.Sum(item => item.VaultInsurance);
        model.PlatformFee = selectedItems.Sum(item => item.PlatformFee);
        model.DepositApplied = selectedItems.Sum(item => item.DepositApplied);
        model.TotalAmount = selectedItems.Sum(item => item.TotalAmount);
        model.SelectedItemCount = selectedItems.Count;
    }

    private static string BuildSubtitle(Product product)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.SetName))
        {
            parts.Add(product.SetName);
        }

        if (!string.IsNullOrWhiteSpace(product.GradeLabel))
        {
            parts.Add(product.GradeLabel);
        }

        if (product.Year.HasValue)
        {
            parts.Add(product.Year.Value.ToString());
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : product.ShortDescription ?? string.Empty;
    }

    private static System.Linq.Expressions.Expression<Func<AuctionOrder, bool>> BuildPayableOrdersFilter(int buyerId, DateTime now) =>
        order =>
            order.BuyerId == buyerId &&
            order.Status == OrderStatuses.PendingPayment &&
            order.DeletedAt == null &&
            order.PaymentDeadline > now &&
            order.Items.Any();
}
