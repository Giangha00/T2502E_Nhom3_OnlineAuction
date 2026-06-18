using Microsoft.EntityFrameworkCore;
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

    public OrderService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
                .ThenInclude(item => item.Auction)
                    .ThenInclude(auction => auction.Product)
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null)
            .OrderBy(order => order.PaymentDeadline)
            .ToListAsync();

        var items = orders
            .SelectMany(order => order.Items.Select(item => new WonOrderItem
            {
                OrderId = order.Id,
                AuctionId = item.AuctionId,
                Name = item.ItemName,
                Subtitle = BuildSubtitle(item.Auction.Product),
                Grade = item.ItemGrade ?? string.Empty,
                ImageUrl = item.ItemImageUrl ?? string.Empty,
                WinningBid = item.WinningBid,
                PaymentDeadline = order.PaymentDeadline,
                OrderReference = order.OrderReference
            }))
            .ToList();

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
            Subtotal = orders.Sum(order => order.Subtotal),
            ShippingFee = orders.Sum(order => order.ShippingFee),
            VaultInsurance = orders.Sum(order => order.VaultInsurance),
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

        model.TotalAmount = model.Subtotal + model.ShippingFee + model.VaultInsurance;
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

        return await _dbContext.Orders
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatuses.Cancelled)
                .SetProperty(order => order.UpdatedAt, now));
    }

    public async Task<int> CancelAllExpiredPendingOrdersAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Orders
            .Where(order =>
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null &&
                order.PaymentDeadline <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatuses.Cancelled)
                .SetProperty(order => order.UpdatedAt, now));
    }

    public async Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        CompleteOrderRequest request)
    {
        if (!SupportedPaymentMethods.Contains(request.PaymentMethod))
        {
            return (false, "Please select a valid payment method.");
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
            return (false, "Please complete all required shipping fields.");
        }

        await CancelExpiredPendingOrdersAsync(buyerId);

        var orders = await _dbContext.Orders
            .Where(order =>
                order.BuyerId == buyerId &&
                order.Status == OrderStatuses.PendingPayment &&
                order.DeletedAt == null)
            .ToListAsync();

        if (orders.Count == 0)
        {
            return (false, "No pending payment orders were found.");
        }

        if (orders.Any(order => order.PaymentDeadline <= DateTime.UtcNow))
        {
            return (false, "One or more payment deadlines have expired.");
        }

        var now = DateTime.UtcNow;
        foreach (var order in orders)
        {
            order.ShippingFullName = fullName;
            order.ShippingAddress = address;
            order.ShippingCity = city;
            order.ShippingPhone = phone;
            order.PaymentMethod = request.PaymentMethod.ToLowerInvariant();
            order.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();

        return (true, "Shipping saved. Payment integration will be available in the next sprint.");
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
