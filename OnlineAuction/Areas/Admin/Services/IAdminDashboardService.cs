using OnlineAuction.Areas.Admin.ViewModels.Dashboard;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ExportSummaryCsvAsync(int periodDays = 30, CancellationToken cancellationToken = default);
}
