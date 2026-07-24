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

    /// <summary>
    /// Always true for auction listings; retained for schema compatibility.
    /// Buy Now listings do not use registration and keep this false.
    /// </summary>
    public bool RequiresRegistration { get; set; } = true;

    public string Status { get; set; } = AuctionStatuses.Live;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int? VerifiedBy { get; set; }

    public string? RejectReason { get; set; }

    public DateTime RegistrationStartDate { get; set; }

    public DateTime RegistrationEndDate { get; set; }

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

    public ICollection<WatchlistItem> WatchlistItems { get; set; } = [];

}

public static class ListingTypes
{
    public const string Auction = "auction";
    public const string BuyNow = "buynow";
}

public static class AuctionStatuses
{
    /// <summary>
    /// Awaiting admin confirmation; not publicly listed.
    /// </summary>
    public const string Confirming = "confirming";

    /// <summary>
    /// Temporary alias for <see cref="Confirming"/> during the pending_review → confirming rename.
    /// </summary>
    public const string PendingReview = Confirming;

    /// <summary>
    /// Legacy DB value before RenamePendingReviewToConfirming migration.
    /// </summary>
    public const string LegacyPendingReview = "pending_review";

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
        Confirming,
        Rejected,
        Scheduled,
        Live,
        EndingSoon,
        Ended,
        AwaitingPayment,
        Completed,
        Cancelled
    ];

    /// <summary>
    /// Statuses that mean "awaiting admin confirmation" (includes pre-migration rows).
    /// </summary>
    public static readonly string[] ConfirmingStatuses =
    [
        Confirming,
        LegacyPendingReview
    ];

    public static bool IsConfirming(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        ConfirmingStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
}
