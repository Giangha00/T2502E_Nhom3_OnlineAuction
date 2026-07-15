namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardRevenueSectionViewModel
{
    public DashboardKpiCardViewModel GmvKpi { get; set; } = new();

    public DashboardKpiCardViewModel CommissionKpi { get; set; } = new();

    public DashboardKpiCardViewModel BuyerFeeKpi { get; set; } = new();

    public DashboardKpiCardViewModel SellerFeeKpi { get; set; } = new();

    public DashboardKpiCardViewModel SellerProceedsKpi { get; set; } = new();
}
