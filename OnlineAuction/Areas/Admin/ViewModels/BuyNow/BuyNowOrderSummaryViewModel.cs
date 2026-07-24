namespace OnlineAuction.Areas.Admin.ViewModels.BuyNow;

public class BuyNowOrderSummaryViewModel
{
    public int OrderId { get; set; }

    public string OrderReference { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}
