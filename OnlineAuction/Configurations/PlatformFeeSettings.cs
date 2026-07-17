namespace OnlineAuction.Configurations;

public class PlatformFeeSettings
{
    public const string SectionName = "PlatformFee";

    /// <summary>
    /// Auction registration deposit as a percentage of estimated value or starting price.
    /// </summary>
    public decimal RegistrationDepositPercent { get; set; } = 10.00m;

    /// <summary>
    /// Buyer checkout fee charged on won-auction and buy-now orders.
    /// </summary>
    public decimal BuyerCheckoutFeePercent { get; set; } = 2.50m;

    /// <summary>
    /// Seller success fee charged when an order is paid.
    /// </summary>
    public decimal SellerSuccessFeePercent { get; set; } = 10.00m;

    /// <summary>
    /// Minimum registration deposit amount (USD).
    /// </summary>
    public decimal MinimumRegistrationDeposit { get; set; } = 1.00m;

    /// <summary>
    /// Legacy listing fee type setting retained for compatibility with older tests and callers.
    /// </summary>
    public string ListingFeeType { get; set; } = ListingFeeTypes.Fixed;

    /// <summary>
    /// Legacy fixed listing fee amount retained for compatibility with older tests and callers.
    /// </summary>
    public decimal ListingFeeAmount { get; set; } = 0m;

    /// <summary>
    /// Legacy percentage listing fee retained for compatibility with older tests and callers.
    /// </summary>
    public decimal ListingFeePercent { get; set; } = 0m;

    /// <summary>
    /// Development only: skip PayPal and auto-approve registration after mock deposit.
    /// </summary>
    public bool UseMockRegistrationDepositPayment { get; set; }
}
