namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRevenueSectionViewModel
{
    public DashboardKpiCardViewModel GmvKpi { get; set; } = new();

    public DashboardKpiCardViewModel PlatformRevenueKpi { get; set; } = new();

    public DashboardKpiCardViewModel ListingFeeKpi { get; set; } = new();

    public DashboardKpiCardViewModel CompletedOrdersKpi { get; set; } = new();

    public IReadOnlyList<DashboardRevenueLinePointViewModel> LineChart { get; set; } = [];

    public DashboardPlatformRevenueBreakdownViewModel PlatformBreakdown { get; set; } = new();

    public IReadOnlyList<DashboardRevenueDetailViewModel> DetailRows { get; set; } = [];

    public bool HasListingFeeData { get; set; }
}
