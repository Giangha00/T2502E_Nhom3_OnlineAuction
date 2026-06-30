namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardCategoryBreakdownViewModel
{
    public int? CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int BidCount { get; set; }

    public decimal BidVolume { get; set; }

    public decimal Percentage { get; set; }
}
