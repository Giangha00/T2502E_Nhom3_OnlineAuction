namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardFilterViewModel
{
    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public string DateRange =>
        OnlineAuction.Helpers.AdminDateRangeHelper.Format(DateFrom, DateTo);

    public string RegistrationGranularity { get; set; } = "day";

    public int PeriodDays => Math.Max(1, (DateTo.Date - DateFrom.Date).Days + 1);
}
