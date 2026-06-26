namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class AdminDashboardViewModel
{
    public IReadOnlyList<DashboardKpiCardViewModel> KpiCards { get; set; } = [];

    public IReadOnlyList<DashboardKpiCardViewModel> SecondaryKpiCards { get; set; } = [];

    public IReadOnlyList<DashboardRecentAuctionViewModel> RecentAuctions { get; set; } = [];

    public IReadOnlyList<DashboardChartPointViewModel> RevenueChart { get; set; } = [];

    public IReadOnlyList<DashboardChartPointViewModel> BidsChart { get; set; } = [];

    public IReadOnlyList<DashboardStatusBreakdownViewModel> AuctionStatusBreakdown { get; set; } = [];
}
