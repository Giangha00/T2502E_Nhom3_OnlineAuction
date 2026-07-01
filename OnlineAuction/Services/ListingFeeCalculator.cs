using OnlineAuction.Configurations;

namespace OnlineAuction.Services;

public static class ListingFeeCalculator
{
    public const decimal MinimumListingFee = 1.00m;

    public static decimal CalculateListingFee(PlatformFeeSettings settings, decimal startingPrice)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (startingPrice <= 0)
        {
            throw new InvalidOperationException("Starting price must be greater than 0 to calculate listing fee.");
        }

        var feeType = NormalizeFeeType(settings.ListingFeeType);
        decimal feeAmount;

        if (feeType == ListingFeeTypes.Percent)
        {
            feeAmount = Math.Round(
                startingPrice * settings.ListingFeePercent / 100m,
                2,
                MidpointRounding.AwayFromZero);
        }
        else
        {
            feeAmount = settings.ListingFeeAmount;
        }

        if (feeAmount < MinimumListingFee)
        {
            feeAmount = MinimumListingFee;
        }

        return feeAmount;
    }

    public static string BuildPreviewDescription(PlatformFeeSettings settings, decimal startingPrice)
    {
        var feeAmount = CalculateListingFee(settings, startingPrice);
        var feeType = NormalizeFeeType(settings.ListingFeeType);

        if (feeType == ListingFeeTypes.Percent)
        {
            return $"${feeAmount:N2} ({settings.ListingFeePercent:N2}% of ${startingPrice:N2} starting price)";
        }

        return $"${feeAmount:N2} (fixed listing fee)";
    }

    public static string NormalizeFeeType(string? feeType) =>
        string.Equals(feeType, ListingFeeTypes.Percent, StringComparison.OrdinalIgnoreCase)
            ? ListingFeeTypes.Percent
            : ListingFeeTypes.Fixed;
}
