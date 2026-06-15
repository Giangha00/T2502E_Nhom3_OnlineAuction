using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class OrderService : IOrderService
{
    private readonly IAuctionService _auctionService;

    public OrderService(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    public OrderPageViewModel BuildOrderPage(ISession session)
    {
        var items = WonOrderStore.GetOrders(session);
        var subtotal = items.Sum(i => i.WinningBid);

        var model = new OrderPageViewModel
        {
            Items = items,
            Subtotal = subtotal,
            ShippingFee = items.Count > 0 ? 45m : 0m,
            VaultInsurance = items.Count > 0 ? Math.Round(Math.Max(60m, subtotal * 0.00721m), 2) : 0m,
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

    public (bool Success, string Message, string? RedirectUrl) PlaceBid(
        ISession session,
        int auctionId,
        decimal amount)
    {
        if (auctionId <= 0 || amount <= 0)
        {
            return (false, "Invalid bid.", null);
        }

        var auction = _auctionService.GetAuctionById(auctionId);
        if (auction is null)
        {
            return (false, "Auction not found.", null);
        }

        var deadlineHours = 2 + (auctionId % 19);
        var order = new WonOrderItem
        {
            AuctionId = auction.Id,
            Name = auction.Year > 0 ? $"{auction.Year} {auction.Name}" : auction.Name,
            Subtitle = BuildSubtitle(auction),
            Grade = auction.Grade,
            ImageUrl = auction.ImageUrl,
            WinningBid = amount,
            PaymentDeadline = DateTime.UtcNow.AddHours(deadlineHours),
            OrderReference = $"AH-{DateTime.UtcNow:yyyyMMdd}-{auction.Id:D4}"
        };

        WonOrderStore.AddOrder(session, order);

        return (true, "Congratulations! You won this auction.", "/Order");
    }

    public (bool Success, string OrderRef, string AuctionName, decimal Total, string Method) CompleteOrder(
        ISession session,
        string paymentMethod)
    {
        var items = WonOrderStore.GetOrders(session);
        if (items.Count == 0)
        {
            return (false, string.Empty, string.Empty, 0, string.Empty);
        }

        var subtotal = items.Sum(i => i.WinningBid);
        var shipping = 45m;
        var insurance = Math.Round(Math.Max(60m, subtotal * 0.00721m), 2);
        var total = subtotal + shipping + insurance;
        var orderRef = items.Count == 1
            ? items[0].OrderReference
            : $"AH-{DateTime.UtcNow:yyyyMMdd}-B{items.Count}";

        var auctionName = items.Count == 1
            ? items[0].Name
            : $"{items.Count} won auctions";

        var method = paymentMethod switch
        {
            "cod" => "Cash on Delivery (COD)",
            _ => "PayPal"
        };

        WonOrderStore.Clear(session);

        return (true, orderRef, auctionName, total, method);
    }

    private static string BuildSubtitle(AuctionItemViewModel auction)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(auction.Subtitle))
        {
            parts.Add(auction.Subtitle.Split('·')[0].Trim());
        }

        if (!string.IsNullOrWhiteSpace(auction.Grade))
        {
            parts.Add($"{auction.Grade} Graded");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : auction.Category;
    }
}
