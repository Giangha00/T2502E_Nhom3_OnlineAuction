using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
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

    public OrderService(
        AuctionHouseDbContext dbContext,
        IOptions<PlatformFeeSettings> feeSettings,
        IWinnerNonPaymentRecoveryService winnerNonPaymentRecoveryService)
    {
        _dbContext = dbContext;
        _feeSettings = feeSettings.Value;
        _winnerNonPaymentRecoveryService = winnerNonPaymentRecoveryService;
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

        var now = DateTime.UtcNow;
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Auction)
                    .ThenInclude(auction => auction.Product)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline > now)
            .OrderBy(order => order.PaymentDeadline)
            .ToListAsync();

        var items = orders
            .Select(order =>
            {
                var item = order.Items.First();
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

    public Task<int> CountPendingPaymentOrdersAsync(int buyerId) =>
        _dbContext.Orders.CountAsync(order =>
            order.BuyerId == buyerId &&
            order.Status == OrderStatuses.PendingPayment &&
            order.DeletedAt == null &&
            order.PaymentDeadline > DateTime.UtcNow);

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
            var references = string.Join(", ", checkoutOrders.Select(order => order.OrderReference));
            return (true, $"Thanh toán thành công! Đã xác nhận {checkoutOrders.Count} hóa đơn ({references}).");
        }

        return (true, "Đã lưu thông tin giao hàng. Bạn sẽ được chuyển đến PayPal để hoàn tất thanh toán.");
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
            }
        }

        await _dbContext.SaveChangesAsync();
        return expiredOrders.Count;
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
}
