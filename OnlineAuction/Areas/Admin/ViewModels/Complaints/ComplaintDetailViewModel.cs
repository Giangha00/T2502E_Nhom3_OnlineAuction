namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public class ComplaintDetailViewModel
{
    public int Id { get; set; }

    public string RequestReference { get; set; } = string.Empty;

    public string ComplaintType { get; set; } = string.Empty;

    public string ComplaintTypeLabel { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public string ReasonLabel { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? RequestedAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string? AdminNotes { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewerName { get; set; }

    public IReadOnlyList<string> EvidenceUrls { get; init; } = [];

    public int? OrderId { get; set; }

    public string? OrderReference { get; set; }

    public decimal? OrderSubtotal { get; set; }

    public decimal? OrderTotal { get; set; }

    public string? OrderStatus { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaidAt { get; set; }

    public int BuyerId { get; set; }

    public string BuyerName { get; set; } = string.Empty;

    public string BuyerEmail { get; set; } = string.Empty;

    public int? SellerId { get; set; }

    public string? SellerName { get; set; }

    public string? SellerEmail { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int? AuctionId { get; set; }

    public bool CanMarkUnderReview { get; set; }

    public bool CanApprove { get; set; }

    public bool CanReject { get; set; }

    public bool CanClose { get; set; }
}
