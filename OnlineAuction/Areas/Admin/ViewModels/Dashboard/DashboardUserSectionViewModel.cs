namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardUserSectionViewModel
{
    public DashboardKpiCardViewModel NewRegistrationsKpi { get; set; } = new();

    public DashboardKpiCardViewModel ActiveUsersKpi { get; set; } = new();

    public IReadOnlyList<DashboardRegistrationChartPointViewModel> RegistrationByDay { get; set; } = [];

    public IReadOnlyList<DashboardRegistrationChartPointViewModel> RegistrationByWeek { get; set; } = [];

    public IReadOnlyList<DashboardRegistrationChartPointViewModel> RegistrationByMonth { get; set; } = [];

    public IReadOnlyList<DashboardTopUserViewModel> TopBuyers { get; set; } = [];

    public IReadOnlyList<DashboardTopUserViewModel> TopSellers { get; set; } = [];
}
