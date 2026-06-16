namespace OnlineAuction.Entities;

public class Auction : AuditableEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public decimal StartingPrice { get; set; }

    public decimal BidStep { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public string Status { get; set; } = AuctionStatuses.Live;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? WinnerId { get; set; }

    public Product Product { get; set; } = null!;

    public ApplicationUser? Winner { get; set; }

    public ICollection<Bid> Bids { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

public static class AuctionStatuses
{
    public const string Live = "live";
    public const string EndingSoon = "ending_soon";
    public const string Ended = "ended";
    public const string AwaitingPayment = "awaiting_payment";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}
