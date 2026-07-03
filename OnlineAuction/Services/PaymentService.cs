using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class PaymentService : IPaymentService
{
    private readonly IAuctionService _auctionService;
    private readonly PlatformFeeSettings _feeSettings;

    public PaymentService(IAuctionService auctionService, IOptions<PlatformFeeSettings> feeSettings)
    {
        _auctionService = auctionService;
        _feeSettings = feeSettings.Value;
    }

    public PaymentInformationViewModel GetPaymentInformation()
    {
        return new PaymentInformationViewModel
        {
            SavedMethods = MockPaymentData.GetSavedPaymentMethods()
        };
    }

    public PaymentCheckoutViewModel? BuildCheckout(int? auctionId)
    {
        var auction = auctionId.HasValue
            ? _auctionService.GetAuctionById(auctionId.Value)
            : _auctionService.GetAllAuctions().FirstOrDefault();

        if (auction is null)
        {
            return null;
        }

        var platformFee = MarketplaceFeeCalculator.CalculateBuyerCheckoutFee(auction.CurrentPrice, _feeSettings);
        var shippingFee = GetShippingFee(auction.Category);
        var total = auction.CurrentPrice + platformFee + shippingFee;

        return new PaymentCheckoutViewModel
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
    }

    public PaymentConfirmationViewModel BuildConfirmation(
        string? orderRef,
        string? auctionName,
        decimal? total,
        string? method)
    {
        return new PaymentConfirmationViewModel
        {
            OrderReference = orderRef ?? "AH-00000000-0000",
            AuctionName = auctionName ?? "Your auction item",
            TotalAmount = total ?? 0,
            PaymentMethod = method ?? "Bank Transfer",
            PaidAt = DateTime.UtcNow
        };
    }

    private static decimal GetShippingFee(string category) =>
        category switch
        {
            "Pokémon" or "One Piece" or "Yu-Gi-Oh!" or "Magic: The Gathering" => 18m,
            "Sports" => 22m,
            _ => 20m
        };
}
