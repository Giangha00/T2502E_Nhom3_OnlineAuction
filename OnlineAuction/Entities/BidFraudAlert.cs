namespace OnlineAuction.Entities;

public class BidFraudAlert
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public long? BidId { get; set; }

    public int? UserId { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public string Severity { get; set; } = FraudAlertSeverities.Medium;

    public string Message { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public string Status { get; set; } = FraudAlertStatuses.Open;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Auction Auction { get; set; } = null!;

    public Bid? Bid { get; set; }

    public ApplicationUser? User { get; set; }

    public ApplicationUser? Reviewer { get; set; }
}

public static class FraudAlertTypes
{
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string SameIpMultipleAccounts = "same_ip_multiple_accounts";
    public const string RapidBidding = "rapid_bidding";
    public const string CollusionRoundTrip = "collusion_round_trip";
    public const string AbnormalPriceJump = "abnormal_price_jump";
    public const string NewAccountHighBid = "new_account_high_bid";
    public const string SellerRelatedBidder = "seller_related_bidder";
}

public static class FraudAlertSeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
}

public static class FraudAlertStatuses
{
    public const string Open = "open";
    public const string Reviewed = "reviewed";
    public const string Dismissed = "dismissed";
}
