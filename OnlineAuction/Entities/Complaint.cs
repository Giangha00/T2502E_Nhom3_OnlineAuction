namespace OnlineAuction.Entities;

public class Complaint : AuditableEntity
{
    public int Id { get; set; }

    public string RequestReference { get; set; } = string.Empty;

    public int? OrderId { get; set; }

    public string? OrderReference { get; set; }

    public int BuyerId { get; set; }

    public string ComplaintType { get; set; } = ComplaintTypes.Refund;

    public string ReasonCode { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? RequestedAmount { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string Status { get; set; } = ComplaintStatuses.Pending;

    public string? AdminNotes { get; set; }

    public string? ResolutionNote { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? EvidenceUrlsJson { get; set; }

    public AuctionOrder? Order { get; set; }

    public ApplicationUser Buyer { get; set; } = null!;

    public ApplicationUser? Reviewer { get; set; }

    public static string BuildRequestReference(int id, DateTime createdAt) =>
        $"RF-{createdAt:yyyyMMdd}-{id}";
}

public static class ComplaintStatuses
{
    public const string Pending = "pending";
    public const string UnderReview = "under_review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Closed = "closed";

    public static readonly string[] All =
    [
        Pending,
        UnderReview,
        Approved,
        Rejected,
        Closed
    ];

    public static readonly string[] OpenStatuses =
    [
        Pending,
        UnderReview
    ];
}

public static class ComplaintTypes
{
    public const string Refund = "refund";
    public const string Dispute = "dispute";
    public const string Authenticity = "authenticity";
    public const string Other = "other";

    public static readonly string[] All =
    [
        Refund,
        Dispute,
        Authenticity,
        Other
    ];
}

public static class ComplaintReasonCodes
{
    public const string NotAsDescribed = "not-as-described";
    public const string Damaged = "damaged";
    public const string NotReceived = "not-received";
    public const string Counterfeit = "counterfeit";
    public const string DuplicatePayment = "duplicate-payment";
    public const string Other = "other";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [NotAsDescribed] = "Item not as described in listing",
        [Damaged] = "Item arrived damaged",
        [NotReceived] = "Item not received within delivery window",
        [Counterfeit] = "Suspected counterfeit or misrepresented item",
        [DuplicatePayment] = "Duplicate or incorrect payment",
        [Other] = "Other (please describe)"
    };
}

public static class ComplaintStatusActions
{
    public const string UnderReview = "under_review";
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const string Close = "close";
    public const string AddNote = "add_note";
}
