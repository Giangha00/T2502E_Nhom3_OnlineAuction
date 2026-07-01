using OnlineAuction.Configurations;

namespace OnlineAuction.Entities;

/// <summary>
/// Platform listing fee charged from the seller when admin approves a listing.
/// Separate from buyer registration deposits (<see cref="AuctionRegistrationDeposit"/>).
/// </summary>
public class ListingFee : AuditableEntity
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public int SellerId { get; set; }

    public decimal FeeAmount { get; set; }

    /// <summary>fixed or percent — snapshot of config at charge time.</summary>
    public string FeeType { get; set; } = ListingFeeTypes.Fixed;

    /// <summary>pending, paid, waived, failed</summary>
    public string Status { get; set; } = ListingFeeStatuses.Pending;

    public DateTime? PaidAt { get; set; }

    public Auction Auction { get; set; } = null!;

    public ApplicationUser Seller { get; set; } = null!;

    public ApplicationUser? CreatedByAdmin { get; set; }
}

public static class ListingFeeStatuses
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Waived = "waived";
    public const string Failed = "failed";
}
