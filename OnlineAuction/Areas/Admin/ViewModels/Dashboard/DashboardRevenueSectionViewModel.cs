namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRevenueSectionViewModel
{
    public DashboardKpiCardViewModel GmvKpi { get; set; } = new();

    public DashboardKpiCardViewModel CommissionKpi { get; set; } = new();
}
