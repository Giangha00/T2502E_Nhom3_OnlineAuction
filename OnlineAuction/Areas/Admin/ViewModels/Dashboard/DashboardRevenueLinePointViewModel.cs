namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRevenueLinePointViewModel
{
    public string Label { get; set; } = string.Empty;

    public string FilterKey { get; set; } = string.Empty;

    public decimal Gmv { get; set; }

    public decimal PlatformRevenue { get; set; }
}
