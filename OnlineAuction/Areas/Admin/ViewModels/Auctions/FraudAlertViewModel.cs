namespace OnlineAuction.Areas.Admin.ViewModels.Auctions;

public sealed class FraudAlertViewModel
{
    public long Id { get; set; }

    public int AuctionId { get; set; }

    public long? BidId { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? ReviewedBy { get; set; }

    public string? ReviewerName { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
