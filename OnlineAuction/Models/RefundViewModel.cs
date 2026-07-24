namespace OnlineAuction.Models;

public class RefundPageViewModel
{
    public bool IsAuthenticated { get; set; }

    public List<RefundEligibleOrderViewModel> RecentOrders { get; set; } = [];
    public List<RefundReasonOption> RefundReasons { get; set; } = [];
    public List<RefundPolicyItem> PolicyItems { get; set; } = [];
}

public class RefundEligibleOrderViewModel
{
    public int OrderId { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public string AuctionName { get; set; } = string.Empty;

    public decimal AmountPaid { get; set; }

    public DateTime PaidOn { get; set; }
}

public class RefundReasonOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class RefundPolicyItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RefundConfirmationViewModel
{
    public string RequestId { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string EstimatedReviewDays { get; set; } = "3–5 business days";
}

public class RefundSubmitViewModel
{
    public int? OrderId { get; set; }

    public string? OrderReference { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? RequestedAmount { get; set; }

    public string? EvidenceUrls { get; set; }
}
