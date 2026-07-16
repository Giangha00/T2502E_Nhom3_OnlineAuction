namespace OnlineAuction.Entities;

/// <summary>
/// Audit trail when an auction-win order expires without payment.
/// </summary>
public class WinnerNonPaymentLog
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public int CancelledOrderId { get; set; }

    public int DefaultingUserId { get; set; }

    public long? ForfeitedDepositId { get; set; }

    public decimal? ForfeitedAmount { get; set; }

    /// <summary>
    /// payment_expired, deposit_forfeited, second_chance_offered, relist_recommended
    /// </summary>
    public string Action { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public int? SecondChanceUserId { get; set; }

    public int? SecondChanceOrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Auction Auction { get; set; } = null!;
}

public static class WinnerNonPaymentActions
{
    public const string PaymentExpired = "payment_expired";
    public const string DepositForfeited = "deposit_forfeited";
    public const string SecondChanceOffered = "second_chance_offered";
    public const string RelistRecommended = "relist_recommended";
}
