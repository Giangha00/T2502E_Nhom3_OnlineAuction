namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRevenueDetailViewModel
{
    public DateTime TransactionDate { get; set; }

    public string Type { get; set; } = string.Empty;

    public string ReferenceCode { get; set; } = string.Empty;

    public int? AuctionId { get; set; }

    public int? OrderId { get; set; }

    public decimal GmvAmount { get; set; }

    public decimal PlatformRevenueAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}
