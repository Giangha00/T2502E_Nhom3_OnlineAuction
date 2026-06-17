namespace OnlineAuction.Entities;

public class OrderItem : AuditableEntity
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int AuctionId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string? ItemGrade { get; set; }

    public string? ItemImageUrl { get; set; }

    public decimal WinningBid { get; set; }

    public AuctionOrder Order { get; set; } = null!;

    public Auction Auction { get; set; } = null!;
}
