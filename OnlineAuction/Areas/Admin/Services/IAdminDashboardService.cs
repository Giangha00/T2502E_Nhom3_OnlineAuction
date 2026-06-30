using OnlineAuction.Areas.Admin.ViewModels.Dashboard;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminDashboardService
{
    DashboardFilterViewModel NormalizeFilter(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? statusFilter = null,
        int? categoryIdFilter = null,
        DateTime? registrationDateFilter = null,
        string? registrationGranularity = null);

    Task<AdminDashboardViewModel> GetDashboardAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<int> GetNewUserRegistrationsCountAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveUsersCountAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardTopUserViewModel>> GetTopBuyersAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardTopUserViewModel>> GetTopSellersAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<(int Ongoing, int Ended, int Cancelled, int PendingReview)> GetAuctionStatusCountsAsync(
        CancellationToken cancellationToken = default);

    Task<decimal?> GetAuctionSuccessRateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardCategoryBreakdownViewModel>> GetCategoryBidBreakdownAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportExcelAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportSummaryCsvAsync(int periodDays = 30, CancellationToken cancellationToken = default);
}
