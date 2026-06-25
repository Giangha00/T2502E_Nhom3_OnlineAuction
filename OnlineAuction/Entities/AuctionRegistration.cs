namespace OnlineAuction.Entities;

public class AuctionRegistration : AuditableEntity
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = AuctionRegistrationStatuses.Pending;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedBy { get; set; }

    public string? RejectReason { get; set; }

    public Auction Auction { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public ApplicationUser? Reviewer { get; set; }
    
    public ICollection<AuctionRegistrationDeposit> Deposits { get; set; } = [];
}

public static class AuctionRegistrationStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
}

