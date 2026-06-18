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
                Subtitle = BuildSubtitle(item.Auction.Product),
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
            order.DeletedAt == null);

    public async Task<(bool Success, string Message)> CompleteOrderAsync(
        int buyerId,
        string paymentMethod)
    {
        if (!SupportedPaymentMethods.Contains(paymentMethod))
        {
            return (false, "Please select a valid payment method.");
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

        foreach (var order in orders)
        {
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return (true, "Payment method saved. Payment integration will be available in the next sprint.");
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
