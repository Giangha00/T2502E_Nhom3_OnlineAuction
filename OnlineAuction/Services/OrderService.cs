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
                Subtitle = item.Auction.Product.Subtitle ?? item.Auction.Product.SetName ?? string.Empty,
                Grade = item.ItemGrade ?? string.Empty,
                ImageUrl = item.ItemImageUrl ?? string.Empty,
                WinningBid = item.WinningBid,
                PaymentDeadline = order.PaymentDeadline,
                OrderReference = order.OrderReference
            }))
            .ToList();

        var model = new OrderPageViewModel
        {
            Items = items,
            FullName = buyer.FullName,
            Phone = buyer.PhoneNumber ?? string.Empty,
            ShippingAddress = orders.FirstOrDefault(order => !string.IsNullOrWhiteSpace(order.ShippingAddress))
                ?.ShippingAddress ?? string.Empty,
            Subtotal = orders.Sum(order => order.Subtotal),
            ShippingFee = orders.Sum(order => order.ShippingFee),
            VaultInsurance = orders.Sum(order => order.VaultInsurance),
            PaymentMethods =
            [
                new PaymentMethodOption
                {
                    Id = "paypal",
                    Name = "PayPal",
                    Description = "Payment integration next sprint"
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
            order.DeletedAt == null);

    public async Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        string shippingAddress,
        string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            return (false, "Shipping address is required.");
        }

        if (!SupportedPaymentMethods.Contains(paymentMethod))
        {
            return (false, "Please select a valid payment method.");
        }

        var buyer = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == buyerId);

        if (buyer is null)
        {
            return (false, "Please sign in to complete your order.");
        }

        if (string.IsNullOrWhiteSpace(buyer.FullName) || string.IsNullOrWhiteSpace(buyer.PhoneNumber))
        {
            return (false, "Please update your profile name and phone number before checkout.");
        }

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

        var trimmedAddress = shippingAddress.Trim();
        foreach (var order in orders)
        {
            order.ShippingAddress = trimmedAddress;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return (true, "Shipping information saved. Payment integration next sprint.");
    }
}
