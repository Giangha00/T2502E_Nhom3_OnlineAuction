namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardKpiCardViewModel
{
    public string Label { get; set; } = string.Empty;

    public string DisplayValue { get; set; } = string.Empty;

    public decimal? ChangePercent { get; set; }

    public string ChangeDisplay { get; set; } = "N/A";

    public bool IsPositiveChange { get; set; }

    public string? LinkUrl { get; set; }

    public string? CardKey { get; set; }
}
