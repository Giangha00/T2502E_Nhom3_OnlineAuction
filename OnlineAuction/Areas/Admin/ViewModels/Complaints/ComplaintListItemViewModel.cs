namespace OnlineAuction.Areas.Admin.ViewModels.Complaints;

public class ComplaintListItemViewModel
{
    public int Id { get; set; }

    public string RequestReference { get; set; } = string.Empty;

    public string? OrderReference { get; set; }

    public string BuyerName { get; set; } = string.Empty;

    public string BuyerEmail { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public string ReasonLabel { get; set; } = string.Empty;

    public decimal? RequestedAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }
}
