using OnlineAuction.Areas.Admin.ViewModels.Dashboard;

namespace OnlineAuction.Areas.Admin.Services;

public interface IAdminDashboardService
{
    DashboardFilterViewModel NormalizeFilter(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? dateRange = null,
        string? statusFilter = null,
        int? categoryIdFilter = null,
        DateTime? registrationDateFilter = null,
        string? registrationGranularity = null,
        string? sectionFilter = null,
        string? revenueTypeFilter = null);

    Task<decimal> SumGmvAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<decimal> SumPlatformRevenueAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardRevenueDetailViewModel>> BuildRevenueDetailTableAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default);

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

    Task<byte[]> ExportSummaryCsvAsync(int periodDays = 7, CancellationToken cancellationToken = default);
}
