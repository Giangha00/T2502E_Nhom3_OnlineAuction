using OnlineAuction.Configurations;
using OnlineAuction.Entities;

namespace OnlineAuction.Services;

public static class MarketplaceFeeCalculator
{
    public static decimal CalculateRegistrationDeposit(decimal productValue, PlatformFeeSettings settings)
    {
        if (productValue <= 0)
        {
            throw new InvalidOperationException("Product value must be greater than zero.");
        }

        var depositAmount = Math.Round(
            productValue * settings.RegistrationDepositPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        if (depositAmount < settings.MinimumRegistrationDeposit)
        {
            depositAmount = settings.MinimumRegistrationDeposit;
        }

        return depositAmount;
    }

    public static decimal CalculateBuyerCheckoutFee(decimal subtotal, PlatformFeeSettings settings) =>
        Math.Round(
            subtotal * settings.BuyerCheckoutFeePercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

    public static decimal CalculateSellerSuccessFee(decimal subtotal, PlatformFeeSettings settings) =>
        Math.Round(
            subtotal * settings.SellerSuccessFeePercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

    /// <summary>
    /// Seller ledger net for Phase 1: Subtotal − SellerSuccessFee.
    /// Shipping, vault insurance, buyer checkout fee, and registration deposit
    /// are not part of seller proceeds.
    /// </summary>
    public static decimal CalculateSellerProceeds(decimal subtotal, PlatformFeeSettings settings)
    {
        var sellerFee = CalculateSellerSuccessFee(subtotal, settings);
        return Math.Max(0m, Math.Round(subtotal - sellerFee, 2, MidpointRounding.AwayFromZero));
    }

    public static void ApplySellerSettlement(AuctionOrder order, PlatformFeeSettings settings)
    {
        order.SellerFee = CalculateSellerSuccessFee(order.Subtotal, settings);
        order.SellerProceeds = CalculateSellerProceeds(order.Subtotal, settings);
    }
}
