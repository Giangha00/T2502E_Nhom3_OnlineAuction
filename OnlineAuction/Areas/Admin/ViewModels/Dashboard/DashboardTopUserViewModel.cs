namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardTopUserViewModel
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int BidCount { get; set; }

    public decimal TotalBidAmount { get; set; }

    public int ListingCount { get; set; }

    public decimal TotalSales { get; set; }
}
