namespace OnlineAuction.Configurations;

public class PlatformFeeSettings
{
    public const string SectionName = "PlatformFee";

    /// <summary>
    /// Seller listing fee charged when admin approves a listing.
    /// Not related to buyer auction registration deposits (AuctionRegistrationDeposit).
    /// </summary>
    public string ListingFeeType { get; set; } = ListingFeeTypes.Fixed;

    public decimal ListingFeeAmount { get; set; } = 5.00m;

    public decimal ListingFeePercent { get; set; } = 2.00m;

    /// <summary>
    /// When true, listing fee is marked paid without PayPal (Development / MVP).
    /// </summary>
    public bool UseMockListingFeePayment { get; set; } = true;
}

public static class ListingFeeTypes
{
    public const string Fixed = "fixed";
    public const string Percent = "percent";
}
