namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class AdminDashboardViewModel
{
    public bool HasValidFilter { get; set; } = true;

    public string? FilterValidationErrorKey { get; set; }

    public DashboardFilterViewModel Filter { get; set; } = new();

    public DashboardRevenueSectionViewModel RevenueSection { get; set; } = new();

    public DashboardUserSectionViewModel UserSection { get; set; } = new();

    public DashboardAuctionSectionViewModel AuctionSection { get; set; } = new();
}
