namespace OnlineAuction.Areas.Admin.ViewModels.Dashboard;

public class DashboardNewUserViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}
