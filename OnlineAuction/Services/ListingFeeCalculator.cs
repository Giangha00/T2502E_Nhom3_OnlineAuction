using OnlineAuction.Configurations;

namespace OnlineAuction.Services;

public static class ListingFeeCalculator
{
    public static decimal CalculateListingFee(PlatformFeeSettings settings, decimal startingPrice)
    {
        if (string.Equals(settings.ListingFeeType, ListingFeeTypes.Fixed, StringComparison.OrdinalIgnoreCase))
        {
            return settings.ListingFeeAmount;
        }

        if (string.Equals(settings.ListingFeeType, ListingFeeTypes.Percent, StringComparison.OrdinalIgnoreCase))
        {
            var fee = startingPrice * (settings.ListingFeePercent / 100m);
            return Math.Max(1m, Math.Round(fee, 2));
        }

        return 0m;
    }
}
