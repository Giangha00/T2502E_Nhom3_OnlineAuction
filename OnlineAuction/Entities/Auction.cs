namespace OnlineAuction.Entities;

public class Auction : AuditableEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public decimal StartingPrice { get; set; }

    public decimal BidStep { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public string ListingType { get; set; } = ListingTypes.Auction;

    public bool RequiresRegistration { get; set; } = true;

    public string Status { get; set; } = AuctionStatuses.Live;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int? VerifiedBy { get; set; }

    public string? RejectReason { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? AuctionEventName { get; set; }

    public int? WinnerId { get; set; }

    public Product Product { get; set; } = null!;

    public ApplicationUser? Winner { get; set; }

    public ApplicationUser? Verifier { get; set; }

    public ICollection<Bid> Bids { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<AuctionRegistration> Registrations { get; set; } = [];
}

public static class ListingTypes
{
    public const string Auction = "auction";
    public const string BuyNow = "buynow";
}

public static class AuctionStatuses
{
    public const string PendingReview = "pending_review";
    public const string Rejected = "rejected";
    public const string Scheduled = "scheduled";
    public const string Live = "live";
    public const string EndingSoon = "ending_soon";
    public const string Ended = "ended";
    public const string AwaitingPayment = "awaiting_payment";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All =
    [
        PendingReview,
        Rejected,
        Scheduled,
        Live,
        EndingSoon,
        Ended,
        AwaitingPayment,
        Completed,
        Cancelled
    ];
}
