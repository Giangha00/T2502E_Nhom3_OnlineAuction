namespace OnlineAuction.Entities;

public class Bid
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public int BidderId { get; set; }

    public decimal Amount { get; set; }

    public bool IsWinning { get; set; }

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    public Auction Auction { get; set; } = null!;

    public ApplicationUser Bidder { get; set; } = null!;
}
