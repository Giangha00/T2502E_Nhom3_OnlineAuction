namespace OnlineAuction.Entities;

public class WatchlistItem
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public int AuctionId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;

    public Auction Auction { get; set; } = null!;
}
