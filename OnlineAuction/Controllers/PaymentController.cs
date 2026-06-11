using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Controllers;

public class PaymentController : Controller
{
    public IActionResult Index()
    {
        var model = new PaymentInformationViewModel
        {
            SavedMethods = MockPaymentData.GetSavedPaymentMethods()
        };

        return View(model);
    }

    public IActionResult Checkout(int? auctionId)
    {
        var auction = auctionId.HasValue
            ? MockAuctionData.GetAuctionById(auctionId.Value)
            : MockAuctionData.GetAllAuctions().FirstOrDefault();

        if (auction is null)
        {
            return NotFound();
        }

        var platformFee = Math.Round(auction.CurrentPrice * 0.025m, 2);
        var shippingFee = GetShippingFee(auction.Category);
        var total = auction.CurrentPrice + platformFee + shippingFee;

        var model = new PaymentCheckoutViewModel
        {
            Auction = auction,
            OrderReference = $"AH-{DateTime.UtcNow:yyyyMMdd}-{auction.Id:D4}",
            PaymentDeadline = DateTime.UtcNow.AddDays(3),
            WinningBid = auction.CurrentPrice,
            PlatformFee = platformFee,
            ShippingFee = shippingFee,
            TotalAmount = total,
            PaymentMethods =
            [
                new PaymentMethodOption
                {
                    Id = "bank-transfer",
                    Name = "Bank Transfer",
                    Description = "Transfer to Auction House escrow account. Processing within 1–2 business days."
                },
                new PaymentMethodOption
                {
                    Id = "card",
                    Name = "Credit / Debit Card",
                    Description = "Visa, Mastercard, and JCB accepted. Instant confirmation."
                },
                new PaymentMethodOption
                {
                    Id = "e-wallet",
                    Name = "E-Wallet",
                    Description = "Pay via MoMo, ZaloPay, or VNPay supported gateways."
                }
            ]
        };

        return View(model);
    }

    public IActionResult Confirmation(string? orderRef, string? auctionName, decimal? total, string? method)
    {
        var model = new PaymentConfirmationViewModel
        {
            OrderReference = orderRef ?? "AH-00000000-0000",
            AuctionName = auctionName ?? "Your auction item",
            TotalAmount = total ?? 0,
            PaymentMethod = method ?? "Bank Transfer"
        };

        return View(model);
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
