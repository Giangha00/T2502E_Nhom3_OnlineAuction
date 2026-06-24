namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRecentAuctionViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;

    public decimal CurrentPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string StatusBadgeClass { get; set; } = string.Empty;

    public string EndsIn { get; set; } = string.Empty;
}
