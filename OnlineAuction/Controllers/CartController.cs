using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class CartController : Controller
{
    public IActionResult Index()
    {
        var wonAuctionIds = new[] { 3, 12 };
        var wonItems = wonAuctionIds
            .Select(id => MockAuctionData.GetAuctionById(id))
            .Where(a => a is not null)
            .Select(a => BuildWonItem(a!))
            .ToList();

        var model = new CartViewModel
        {
            WatchingItems = [],
            WonItems = wonItems,
            AllAuctions = MockAuctionData.GetAllAuctions(),
            TotalPendingPayment = wonItems.Sum(i => i.TotalDue)
        };

        return View(model);
    }

    private static CartItemViewModel BuildWonItem(AuctionItemViewModel auction)
    {
        var platformFee = Math.Round(auction.CurrentPrice * 0.025m, 2);
        var shippingFee = GetShippingFee(auction.Category);

        return new CartItemViewModel
        {
            Auction = new AuctionItemViewModel
            {
                Id = auction.Id,
                Name = auction.Name,
                Category = auction.Category,
                ImageUrl = auction.ImageUrl,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                Status = "Won",
                TimeRemaining = "Payment due"
            },
            PaymentDeadline = DateTime.UtcNow.AddDays(2),
            PlatformFee = platformFee,
            ShippingFee = shippingFee,
            TotalDue = auction.CurrentPrice + platformFee + shippingFee
        };
    }

    private static decimal GetShippingFee(string category) =>
        category switch
        {
            "Cars" => 850m,
            "Jewelry" => 45m,
            "Watches" => 35m,
            "Cards" => 25m,
            "Billiard Sticks" => 30m,
            _ => 40m
        };
}
