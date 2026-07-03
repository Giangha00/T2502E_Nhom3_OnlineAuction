using OnlineAuction.Configurations;

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
}
