namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardAuctionSectionViewModel
{
    public DashboardKpiCardViewModel OngoingKpi { get; set; } = new();

    public DashboardKpiCardViewModel EndedKpi { get; set; } = new();

    public DashboardKpiCardViewModel CancelledKpi { get; set; } = new();

    public DashboardKpiCardViewModel SuccessRateKpi { get; set; } = new();

    public IReadOnlyList<DashboardCategoryBreakdownViewModel> CategoryBreakdown { get; set; } = [];
}
