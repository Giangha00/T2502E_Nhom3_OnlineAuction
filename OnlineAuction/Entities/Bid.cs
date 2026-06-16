namespace OnlineAuction.Entities;

public class Bid : AuditableEntity
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public int BidderId { get; set; }

    public decimal Amount { get; set; }

    public string BidType { get; set; } = BidTypes.Manual;

    public bool IsWinning { get; set; }

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    public Auction Auction { get; set; } = null!;

    public ApplicationUser Bidder { get; set; } = null!;
}

public static class BidTypes
{
    public const string Manual = "manual";
    public const string BuyNow = "buy_now";
}
