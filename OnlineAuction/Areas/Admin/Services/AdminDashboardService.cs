using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Dashboard;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private const int TopUserCount = 10;
    private const int CategoryTopCount = 5;
    private const int DefaultFilterDays = DashboardFilterValidator.DefaultFilterDays;

    private static readonly string[] OngoingAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Scheduled
    ];

    private static readonly string[] EndedAuctionStatuses =
    [
        AuctionStatuses.Ended,
        AuctionStatuses.AwaitingPayment,
        AuctionStatuses.Completed
    ];

    private static readonly string[] CancelledAuctionStatuses =
    [
        AuctionStatuses.Cancelled,
        AuctionStatuses.Rejected
    ];

    private static readonly string[] SuccessRateDenominatorStatuses =
    [
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Ended,
        AuctionStatuses.AwaitingPayment,
        AuctionStatuses.Completed,
        AuctionStatuses.Cancelled
    ];

    private static readonly string[] PaidOrderStatuses =
    [
        OrderStatuses.Paid,
        OrderStatuses.Delivered
    ];

    private readonly AuctionHouseDbContext _dbContext;

    public AdminDashboardService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DashboardFilterViewModel NormalizeFilter(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? dateRange = null,
        string? registrationGranularity = null)
    {
        if (!string.IsNullOrWhiteSpace(dateRange))
        {
            var parsed = AdminDateRangeHelper.Parse(dateRange);
            if (parsed.StartDate.HasValue && parsed.EndDateExclusive.HasValue)
            {
                dateFrom = parsed.StartDate;
                dateTo = parsed.EndDateExclusive.Value.AddDays(-1);
            }
        }

        var endDate = (dateTo ?? DateTime.UtcNow).Date;
        var startDate = (dateFrom ?? endDate.AddDays(-(DefaultFilterDays - 1))).Date;

        var granularity = string.IsNullOrWhiteSpace(registrationGranularity)
            ? "day"
            : registrationGranularity.Trim().ToLowerInvariant();

        if (granularity is not ("day" or "week" or "month"))
        {
            granularity = "day";
        }

        return new DashboardFilterViewModel
        {
            DateFrom = startDate,
            DateTo = endDate,
            RegistrationGranularity = granularity
        };
    }

    public async Task<decimal> SumGmvAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var paymentsGmv = await SumSuccessfulPaymentsAsync(rangeStart, rangeEndExclusive, cancellationToken);
        var orphanPaidOrdersGmv = await SumOrphanPaidOrderTotalsAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);

        return paymentsGmv + orphanPaidOrdersGmv;
    }

    public async Task<decimal> SumCommissionAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var buyerCheckoutFees = await SumPaidOrderPlatformFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);
        var sellerSuccessFees = await SumPaidOrderSellerFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);

        return buyerCheckoutFees + sellerSuccessFees;
    }

    public Task<decimal> SumBuyerCheckoutFeesAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);
        return SumPaidOrderPlatformFeesAsync(rangeStart, rangeEndExclusive, cancellationToken);
    }

    public Task<decimal> SumSellerSuccessFeesAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);
        return SumPaidOrderSellerFeesAsync(rangeStart, rangeEndExclusive, cancellationToken);
    }

    public Task<decimal> SumSellerProceedsAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);
        return SumPaidOrderSellerProceedsAsync(rangeStart, rangeEndExclusive, cancellationToken);
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var previousRange = BuildPreviousRange(filter);

        var newRegistrationsCurrent = await GetNewUserRegistrationsCountAsync(filter, cancellationToken);
        var newRegistrationsPrevious = await GetNewUserRegistrationsCountAsync(previousRange, cancellationToken);
        var activeUsersCurrent = await GetActiveUsersCountAsync(filter, cancellationToken);
        var activeUsersPrevious = await GetActiveUsersCountAsync(previousRange, cancellationToken);

        var registrationDates = await GetRegistrationDatesAsync(filter, cancellationToken);
        var statusCounts = await GetAuctionStatusCountsAsync(cancellationToken);
        var pendingVerification = await GetPendingVerificationCountAsync(cancellationToken);
        var successRate = await GetAuctionSuccessRateAsync(cancellationToken);
        var revenueSection = await BuildRevenueSectionAsync(filter, previousRange, cancellationToken);

        return new AdminDashboardViewModel
        {
            Filter = filter,
            RevenueSection = revenueSection,
            UserSection = new DashboardUserSectionViewModel
            {
                NewRegistrationsKpi = BuildKpiCard(
                    "New Registrations",
                    FormatInteger(newRegistrationsCurrent),
                    newRegistrationsCurrent,
                    newRegistrationsPrevious),
                ActiveUsersKpi = BuildKpiCard(
                    "Active Users",
                    FormatInteger(activeUsersCurrent),
                    activeUsersCurrent,
                    activeUsersPrevious),
                RegistrationByDay = BuildRegistrationSeries(registrationDates, "day", filter),
                RegistrationByWeek = BuildRegistrationSeries(registrationDates, "week", filter),
                RegistrationByMonth = BuildRegistrationSeries(registrationDates, "month", filter),
                TopBuyers = await GetTopBuyersAsync(filter, cancellationToken),
                TopSellers = await GetTopSellersAsync(filter, cancellationToken)
            },
            AuctionSection = new DashboardAuctionSectionViewModel
            {
                OngoingKpi = BuildSnapshotKpi(
                    "Ongoing Auctions",
                    FormatInteger(statusCounts.Ongoing),
                    statusCounts.Ongoing),
                EndedKpi = BuildSnapshotKpi(
                    "Ended Auctions",
                    FormatInteger(statusCounts.Ended),
                    statusCounts.Ended),
                CancelledKpi = BuildSnapshotKpi(
                    "Cancelled Auctions",
                    FormatInteger(statusCounts.Cancelled),
                    statusCounts.Cancelled),
                PendingVerificationKpi = BuildSnapshotKpi(
                    "Pending Verification",
                    FormatInteger(pendingVerification),
                    pendingVerification),
                SuccessRateKpi = BuildSnapshotKpi(
                    "Success Rate",
                    $"{successRate:0.0}%",
                    successRate),
                CategoryBreakdown = await GetCategoryBidBreakdownAsync(filter, cancellationToken)
            }
        };
    }

    public Task<int> GetNewUserRegistrationsCountAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return _dbContext.Users.AsNoTracking()
            .CountAsync(
                user => user.DeletedAt == null
                        && user.Status == UserStatus.Active
                        && user.CreatedAt >= rangeStart
                        && user.CreatedAt < rangeEndExclusive,
                cancellationToken);
    }

    public async Task<int> GetActiveUsersCountAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var bidderIds = await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.Bidder.DeletedAt == null
                          && bid.PlacedAt >= rangeStart
                          && bid.PlacedAt < rangeEndExclusive)
            .Select(bid => bid.BidderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var buyerIds = await _dbContext.Orders.AsNoTracking()
            .Where(order => order.DeletedAt == null
                            && order.Buyer.DeletedAt == null
                            && PaidOrderStatuses.Contains(order.Status)
                            && order.CreatedAt >= rangeStart
                            && order.CreatedAt < rangeEndExclusive)
            .Select(order => order.BuyerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return bidderIds.Union(buyerIds).Distinct().Count();
    }

    public async Task<IReadOnlyList<DashboardTopUserViewModel>> GetTopBuyersAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.Bidder.DeletedAt == null
                          && bid.PlacedAt >= rangeStart
                          && bid.PlacedAt < rangeEndExclusive)
            .GroupBy(bid => new { bid.BidderId, bid.Bidder.FullName })
            .Select(group => new DashboardTopUserViewModel
            {
                UserId = group.Key.BidderId,
                FullName = group.Key.FullName,
                BidCount = group.Count(),
                TotalBidAmount = group.Sum(bid => bid.Amount)
            })
            .OrderByDescending(item => item.TotalBidAmount)
            .ThenByDescending(item => item.BidCount)
            .Take(TopUserCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardTopUserViewModel>> GetTopSellersAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var listingCounts = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null
                              && auction.Product.DeletedAt == null
                              && auction.CreatedAt >= rangeStart
                              && auction.CreatedAt < rangeEndExclusive)
            .GroupBy(auction => auction.Product.SellerId)
            .Select(group => new { SellerId = group.Key, ListingCount = group.Count() })
            .ToListAsync(cancellationToken);

        var salesRows = await _dbContext.OrderItems.AsNoTracking()
            .Where(item => item.DeletedAt == null
                           && item.Order.DeletedAt == null
                           && PaidOrderStatuses.Contains(item.Order.Status)
                           && item.Order.CreatedAt >= rangeStart
                           && item.Order.CreatedAt < rangeEndExclusive)
            .Select(item => new
            {
                SellerId = item.Auction.Product.SellerId,
                item.OrderId,
                item.Order.SellerProceeds,
                item.WinningBid
            })
            .ToListAsync(cancellationToken);

        var salesTotals = salesRows
            .GroupBy(item => item.SellerId)
            .Select(group => new
            {
                SellerId = group.Key,
                TotalSales = group
                    .GroupBy(row => row.OrderId)
                    .Sum(orderGroup => orderGroup.First().SellerProceeds),
                GrossSales = group.Sum(row => row.WinningBid)
            })
            .ToList();

        var sellerIds = listingCounts.Select(item => item.SellerId)
            .Union(salesTotals.Select(item => item.SellerId))
            .Distinct()
            .ToList();

        if (sellerIds.Count == 0)
        {
            return [];
        }

        var sellerNames = await _dbContext.Users.AsNoTracking()
            .Where(user => sellerIds.Contains(user.Id) && user.DeletedAt == null)
            .Select(user => new { user.Id, user.FullName })
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        var listingLookup = listingCounts.ToDictionary(item => item.SellerId, item => item.ListingCount);
        var salesLookup = salesTotals.ToDictionary(item => item.SellerId, item => item.TotalSales);
        var grossLookup = salesTotals.ToDictionary(item => item.SellerId, item => item.GrossSales);

        return sellerIds
            .Select(sellerId => new DashboardTopUserViewModel
            {
                UserId = sellerId,
                FullName = sellerNames.GetValueOrDefault(sellerId, "Unknown"),
                ListingCount = listingLookup.GetValueOrDefault(sellerId),
                TotalSales = salesLookup.GetValueOrDefault(sellerId),
                GrossSales = grossLookup.GetValueOrDefault(sellerId)
            })
            .OrderByDescending(item => item.TotalSales)
            .ThenByDescending(item => item.ListingCount)
            .Take(TopUserCount)
            .ToList();
    }

    public async Task<(int Ongoing, int Ended, int Cancelled)> GetAuctionStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null)
            .GroupBy(auction => auction.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var lookup = grouped.ToDictionary(item => item.Status, item => item.Count);

        var ongoing = OngoingAuctionStatuses.Sum(status => lookup.GetValueOrDefault(status));
        var ended = EndedAuctionStatuses.Sum(status => lookup.GetValueOrDefault(status));
        var cancelled = CancelledAuctionStatuses.Sum(status => lookup.GetValueOrDefault(status));

        return (ongoing, ended, cancelled);
    }

    public Task<int> GetPendingVerificationCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && auction.Product.DeletedAt == null
                           && auction.Status == AuctionStatuses.PendingReview,
                cancellationToken);
    }

    public async Task<decimal> GetAuctionSuccessRateAsync(CancellationToken cancellationToken = default)
    {
        var denominator = await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && auction.Product.DeletedAt == null
                           && SuccessRateDenominatorStatuses.Contains(auction.Status),
                cancellationToken);

        if (denominator == 0)
        {
            return 0m;
        }

        var numerator = await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && auction.Product.DeletedAt == null
                           && SuccessRateDenominatorStatuses.Contains(auction.Status)
                           && auction.OrderItems.Any(item =>
                               item.DeletedAt == null
                               && item.Order.DeletedAt == null
                               && PaidOrderStatuses.Contains(item.Order.Status)),
                cancellationToken);

        return Math.Round(numerator / (decimal)denominator * 100m, 1);
    }

    public async Task<IReadOnlyList<DashboardCategoryBreakdownViewModel>> GetCategoryBidBreakdownAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var grouped = await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.Auction.DeletedAt == null
                          && bid.Auction.Product.DeletedAt == null
                          && bid.PlacedAt >= rangeStart
                          && bid.PlacedAt < rangeEndExclusive)
            .GroupBy(bid => new
            {
                bid.Auction.Product.CategoryId,
                bid.Auction.Product.Category.Name
            })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                BidCount = group.Count(),
                BidVolume = group.Sum(bid => bid.Amount)
            })
            .OrderByDescending(item => item.BidVolume)
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return [];
        }

        var totalVolume = grouped.Sum(item => item.BidVolume);
        var topCategories = grouped.Take(CategoryTopCount).ToList();
        var otherCategories = grouped.Skip(CategoryTopCount).ToList();

        var result = topCategories
            .Select(item => new DashboardCategoryBreakdownViewModel
            {
                CategoryId = item.CategoryId,
                CategoryName = item.Name,
                BidCount = item.BidCount,
                BidVolume = item.BidVolume,
                Percentage = totalVolume == 0
                    ? 0
                    : Math.Round(item.BidVolume / totalVolume * 100m, 1)
            })
            .ToList();

        if (otherCategories.Count > 0)
        {
            var otherVolume = otherCategories.Sum(item => item.BidVolume);
            result.Add(new DashboardCategoryBreakdownViewModel
            {
                CategoryId = null,
                CategoryName = "Other",
                BidCount = otherCategories.Sum(item => item.BidCount),
                BidVolume = otherVolume,
                Percentage = totalVolume == 0
                    ? 0
                    : Math.Round(otherVolume / totalVolume * 100m, 1)
            });
        }

        return result;
    }

    public async Task<byte[]> ExportExcelAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await GetDashboardAsync(filter, cancellationToken);
        var previousRange = BuildPreviousRange(filter);
        var generatedAtUtc = DateTime.UtcNow;

        var statusBreakdown = await GetAuctionStatusBreakdownForExportAsync(cancellationToken);
        var categoryBreakdown = await GetFullCategoryBidBreakdownForExportAsync(filter, cancellationToken);
        var listings = await GetListingsInRangeForExportAsync(filter, cancellationToken);
        var orders = await GetOrdersInRangeForExportAsync(filter, cancellationToken);
        var payments = await GetPaymentsInRangeForExportAsync(filter, cancellationToken);
        var dailyRevenue = await GetDailyRevenueForExportAsync(filter, cancellationToken);
        var newUsers = await GetNewUsersInRangeForExportAsync(filter, cancellationToken);

        using var workbook = new XLWorkbook();

        WriteOverviewSheet(workbook, filter, previousRange, dashboard, generatedAtUtc);
        WriteRevenueSheet(workbook, dashboard, dailyRevenue);
        WriteUsersSheet(workbook, dashboard, newUsers);
        WriteAuctionSnapshotSheet(workbook, dashboard, statusBreakdown);
        WriteCategorySheet(workbook, categoryBreakdown);
        WriteListingsSheet(workbook, listings);
        WriteOrdersSheet(workbook, orders);
        WritePaymentsSheet(workbook, payments);

        foreach (var worksheet in workbook.Worksheets)
        {
            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents(1, 80);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportSummaryCsvAsync(int periodDays = 30, CancellationToken cancellationToken = default)
    {
        var filter = NormalizeFilter(
            DateTime.UtcNow.Date.AddDays(-(periodDays - 1)),
            DateTime.UtcNow.Date);

        return await ExportExcelAsync(filter, cancellationToken);
    }

    private static void WriteOverviewSheet(
        XLWorkbook workbook,
        DashboardFilterViewModel filter,
        DashboardFilterViewModel previousRange,
        AdminDashboardViewModel dashboard,
        DateTime generatedAtUtc)
    {
        var sheet = workbook.Worksheets.Add("Overview");
        WriteHeader(sheet, 1, ["Field", "Value"]);
        sheet.Cell(2, 1).Value = "Report";
        sheet.Cell(2, 2).Value = "Admin Dashboard Export";
        sheet.Cell(3, 1).Value = "Date From (UTC)";
        sheet.Cell(3, 2).Value = filter.DateFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(4, 1).Value = "Date To (UTC)";
        sheet.Cell(4, 2).Value = filter.DateTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(5, 1).Value = "Period Days";
        sheet.Cell(5, 2).Value = filter.PeriodDays;
        sheet.Cell(6, 1).Value = "Previous Period From";
        sheet.Cell(6, 2).Value = previousRange.DateFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(7, 1).Value = "Previous Period To";
        sheet.Cell(7, 2).Value = previousRange.DateTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(8, 1).Value = "Generated At (UTC)";
        sheet.Cell(8, 2).Value = generatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        WriteHeader(sheet, 10, ["Section", "Metric", "Display Value", "Numeric Value", "Change vs Previous"]);
        var row = 11;
        void AddKpi(string section, DashboardKpiCardViewModel kpi)
        {
            sheet.Cell(row, 1).Value = section;
            sheet.Cell(row, 2).Value = kpi.Label;
            sheet.Cell(row, 3).Value = kpi.DisplayValue;
            sheet.Cell(row, 4).Value = kpi.NumericValue;
            sheet.Cell(row, 5).Value = string.IsNullOrWhiteSpace(kpi.ChangeDisplay) ? "N/A (snapshot)" : kpi.ChangeDisplay;
            row++;
        }

        AddKpi("Revenue", dashboard.RevenueSection.GmvKpi);
        AddKpi("Revenue", dashboard.RevenueSection.CommissionKpi);
        AddKpi("Revenue", dashboard.RevenueSection.BuyerFeeKpi);
        AddKpi("Revenue", dashboard.RevenueSection.SellerFeeKpi);
        AddKpi("Revenue", dashboard.RevenueSection.SellerProceedsKpi);
        AddKpi("Users", dashboard.UserSection.NewRegistrationsKpi);
        AddKpi("Users", dashboard.UserSection.ActiveUsersKpi);
        AddKpi("Auctions", dashboard.AuctionSection.OngoingKpi);
        AddKpi("Auctions", dashboard.AuctionSection.EndedKpi);
        AddKpi("Auctions", dashboard.AuctionSection.CancelledKpi);
        AddKpi("Auctions", dashboard.AuctionSection.PendingVerificationKpi);
        AddKpi("Auctions", dashboard.AuctionSection.SuccessRateKpi);
    }

    private static void WriteRevenueSheet(
        XLWorkbook workbook,
        AdminDashboardViewModel dashboard,
        IReadOnlyList<DashboardExportDailyRevenueRow> dailyRevenue)
    {
        var sheet = workbook.Worksheets.Add("Revenue");
        WriteHeader(sheet, 1, ["Metric", "Display Value", "Numeric Value", "Change vs Previous"]);

        var revenueKpis = new[]
        {
            dashboard.RevenueSection.GmvKpi,
            dashboard.RevenueSection.CommissionKpi,
            dashboard.RevenueSection.BuyerFeeKpi,
            dashboard.RevenueSection.SellerFeeKpi,
            dashboard.RevenueSection.SellerProceedsKpi
        };

        var row = 2;
        foreach (var kpi in revenueKpis)
        {
            sheet.Cell(row, 1).Value = kpi.Label;
            sheet.Cell(row, 2).Value = kpi.DisplayValue;
            sheet.Cell(row, 3).Value = kpi.NumericValue;
            sheet.Cell(row, 4).Value = kpi.ChangeDisplay;
            row++;
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Daily Revenue (from successful payments)";
        row++;
        WriteHeader(sheet, row, ["Date (UTC)", "Payment Count", "GMV Amount"]);
        row++;

        if (dailyRevenue.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No payment data for selected period";
            return;
        }

        foreach (var day in dailyRevenue)
        {
            sheet.Cell(row, 1).Value = day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sheet.Cell(row, 2).Value = day.PaymentCount;
            sheet.Cell(row, 3).Value = day.Amount;
            row++;
        }
    }

    private static void WriteUsersSheet(
        XLWorkbook workbook,
        AdminDashboardViewModel dashboard,
        IReadOnlyList<DashboardExportUserRow> newUsers)
    {
        var sheet = workbook.Worksheets.Add("Users");
        WriteHeader(sheet, 1, ["Metric", "Display Value", "Numeric Value", "Change vs Previous"]);
        sheet.Cell(2, 1).Value = dashboard.UserSection.NewRegistrationsKpi.Label;
        sheet.Cell(2, 2).Value = dashboard.UserSection.NewRegistrationsKpi.DisplayValue;
        sheet.Cell(2, 3).Value = dashboard.UserSection.NewRegistrationsKpi.NumericValue;
        sheet.Cell(2, 4).Value = dashboard.UserSection.NewRegistrationsKpi.ChangeDisplay;
        sheet.Cell(3, 1).Value = dashboard.UserSection.ActiveUsersKpi.Label;
        sheet.Cell(3, 2).Value = dashboard.UserSection.ActiveUsersKpi.DisplayValue;
        sheet.Cell(3, 3).Value = dashboard.UserSection.ActiveUsersKpi.NumericValue;
        sheet.Cell(3, 4).Value = dashboard.UserSection.ActiveUsersKpi.ChangeDisplay;

        var row = 5;
        sheet.Cell(row, 1).Value = "Registrations By Day";
        row++;
        WriteHeader(sheet, row, ["Label", "Count", "Filter Key"]);
        row++;
        foreach (var point in dashboard.UserSection.RegistrationByDay)
        {
            sheet.Cell(row, 1).Value = point.Label;
            sheet.Cell(row, 2).Value = point.Value;
            sheet.Cell(row, 3).Value = point.FilterKey;
            row++;
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Registrations By Week";
        row++;
        WriteHeader(sheet, row, ["Label", "Count", "Filter Key"]);
        row++;
        foreach (var point in dashboard.UserSection.RegistrationByWeek)
        {
            sheet.Cell(row, 1).Value = point.Label;
            sheet.Cell(row, 2).Value = point.Value;
            sheet.Cell(row, 3).Value = point.FilterKey;
            row++;
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Registrations By Month";
        row++;
        WriteHeader(sheet, row, ["Label", "Count", "Filter Key"]);
        row++;
        foreach (var point in dashboard.UserSection.RegistrationByMonth)
        {
            sheet.Cell(row, 1).Value = point.Label;
            sheet.Cell(row, 2).Value = point.Value;
            sheet.Cell(row, 3).Value = point.FilterKey;
            row++;
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Top Buyers";
        row++;
        WriteHeader(sheet, row, ["User Id", "Full Name", "Bid Count", "Total Bid Amount"]);
        row++;
        if (dashboard.UserSection.TopBuyers.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No buyer activity for selected period";
            row++;
        }
        else
        {
            foreach (var buyer in dashboard.UserSection.TopBuyers)
            {
                sheet.Cell(row, 1).Value = buyer.UserId;
                sheet.Cell(row, 2).Value = buyer.FullName;
                sheet.Cell(row, 3).Value = buyer.BidCount;
                sheet.Cell(row, 4).Value = buyer.TotalBidAmount;
                row++;
            }
        }

        row += 1;
        sheet.Cell(row, 1).Value = "Top Sellers";
        row++;
        WriteHeader(sheet, row, ["User Id", "Full Name", "Listing Count", "Seller Proceeds", "Gross Sales"]);
        row++;
        if (dashboard.UserSection.TopSellers.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No seller activity for selected period";
            row++;
        }
        else
        {
            foreach (var seller in dashboard.UserSection.TopSellers)
            {
                sheet.Cell(row, 1).Value = seller.UserId;
                sheet.Cell(row, 2).Value = seller.FullName;
                sheet.Cell(row, 3).Value = seller.ListingCount;
                sheet.Cell(row, 4).Value = seller.TotalSales;
                sheet.Cell(row, 5).Value = seller.GrossSales;
                row++;
            }
        }

        row += 1;
        sheet.Cell(row, 1).Value = "New Registered Users (detail)";
        row++;
        WriteHeader(sheet, row, ["User Id", "Full Name", "Email", "Phone", "Role", "Status", "Created At (UTC)"]);
        row++;
        if (newUsers.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No new registrations for selected period";
            return;
        }

        foreach (var user in newUsers)
        {
            sheet.Cell(row, 1).Value = user.UserId;
            sheet.Cell(row, 2).Value = user.FullName;
            sheet.Cell(row, 3).Value = user.Email;
            sheet.Cell(row, 4).Value = user.PhoneNumber;
            sheet.Cell(row, 5).Value = user.Role;
            sheet.Cell(row, 6).Value = user.Status;
            sheet.Cell(row, 7).Value = user.CreatedAt;
            row++;
        }
    }

    private static void WriteAuctionSnapshotSheet(
        XLWorkbook workbook,
        AdminDashboardViewModel dashboard,
        IReadOnlyList<DashboardExportStatusRow> statusBreakdown)
    {
        var sheet = workbook.Worksheets.Add("Auction Snapshot");
        WriteHeader(sheet, 1, ["Metric", "Display Value", "Numeric Value", "Scope"]);
        sheet.Cell(2, 1).Value = dashboard.AuctionSection.OngoingKpi.Label;
        sheet.Cell(2, 2).Value = dashboard.AuctionSection.OngoingKpi.DisplayValue;
        sheet.Cell(2, 3).Value = dashboard.AuctionSection.OngoingKpi.NumericValue;
        sheet.Cell(2, 4).Value = "Snapshot now";
        sheet.Cell(3, 1).Value = dashboard.AuctionSection.EndedKpi.Label;
        sheet.Cell(3, 2).Value = dashboard.AuctionSection.EndedKpi.DisplayValue;
        sheet.Cell(3, 3).Value = dashboard.AuctionSection.EndedKpi.NumericValue;
        sheet.Cell(3, 4).Value = "Snapshot now";
        sheet.Cell(4, 1).Value = dashboard.AuctionSection.CancelledKpi.Label;
        sheet.Cell(4, 2).Value = dashboard.AuctionSection.CancelledKpi.DisplayValue;
        sheet.Cell(4, 3).Value = dashboard.AuctionSection.CancelledKpi.NumericValue;
        sheet.Cell(4, 4).Value = "Snapshot now";
        sheet.Cell(5, 1).Value = dashboard.AuctionSection.PendingVerificationKpi.Label;
        sheet.Cell(5, 2).Value = dashboard.AuctionSection.PendingVerificationKpi.DisplayValue;
        sheet.Cell(5, 3).Value = dashboard.AuctionSection.PendingVerificationKpi.NumericValue;
        sheet.Cell(5, 4).Value = "Snapshot now";
        sheet.Cell(6, 1).Value = dashboard.AuctionSection.SuccessRateKpi.Label;
        sheet.Cell(6, 2).Value = dashboard.AuctionSection.SuccessRateKpi.DisplayValue;
        sheet.Cell(6, 3).Value = dashboard.AuctionSection.SuccessRateKpi.NumericValue;
        sheet.Cell(6, 4).Value = "Snapshot now";

        WriteHeader(sheet, 8, ["Status (DB)", "Status Label", "Count", "Bucket"]);
        var row = 9;
        if (statusBreakdown.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No listings in database";
            return;
        }

        foreach (var item in statusBreakdown)
        {
            sheet.Cell(row, 1).Value = item.Status;
            sheet.Cell(row, 2).Value = item.StatusLabel;
            sheet.Cell(row, 3).Value = item.Count;
            sheet.Cell(row, 4).Value = item.Bucket;
            row++;
        }
    }

    private static void WriteCategorySheet(
        XLWorkbook workbook,
        IReadOnlyList<DashboardCategoryBreakdownViewModel> categories)
    {
        var sheet = workbook.Worksheets.Add("Category Bids");
        WriteHeader(sheet, 1, ["Category Id", "Category", "Bid Count", "Bid Volume", "Share %"]);
        var row = 2;
        if (categories.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No bid data for selected period";
            return;
        }

        foreach (var category in categories)
        {
            sheet.Cell(row, 1).Value = category.CategoryId;
            sheet.Cell(row, 2).Value = category.CategoryName;
            sheet.Cell(row, 3).Value = category.BidCount;
            sheet.Cell(row, 4).Value = category.BidVolume;
            sheet.Cell(row, 5).Value = category.Percentage;
            row++;
        }
    }

    private static void WriteListingsSheet(
        XLWorkbook workbook,
        IReadOnlyList<DashboardExportListingRow> listings)
    {
        var sheet = workbook.Worksheets.Add("Listings");
        WriteHeader(sheet, 1,
        [
            "Listing Id", "Product Id", "Product", "Listing Type", "Status", "Status Label",
            "Category", "Seller Id", "Seller", "Starting Price", "Current Price", "Buy Now Price",
            "Bid Count", "Registration Count", "Winner Id", "Registration Start", "Registration End",
            "Start Date", "End Date", "Created At (UTC)"
        ]);

        var row = 2;
        if (listings.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No listings created in selected period";
            return;
        }

        foreach (var listing in listings)
        {
            sheet.Cell(row, 1).Value = listing.ListingId;
            sheet.Cell(row, 2).Value = listing.ProductId;
            sheet.Cell(row, 3).Value = listing.ProductName;
            sheet.Cell(row, 4).Value = listing.ListingType;
            sheet.Cell(row, 5).Value = listing.Status;
            sheet.Cell(row, 6).Value = listing.StatusLabel;
            sheet.Cell(row, 7).Value = listing.CategoryName;
            sheet.Cell(row, 8).Value = listing.SellerId;
            sheet.Cell(row, 9).Value = listing.SellerName;
            sheet.Cell(row, 10).Value = listing.StartingPrice;
            sheet.Cell(row, 11).Value = listing.CurrentPrice;
            sheet.Cell(row, 12).Value = listing.BuyNowPrice;
            sheet.Cell(row, 13).Value = listing.BidCount;
            sheet.Cell(row, 14).Value = listing.RegistrationCount;
            sheet.Cell(row, 15).Value = listing.WinnerId;
            sheet.Cell(row, 16).Value = listing.RegistrationStartDate;
            sheet.Cell(row, 17).Value = listing.RegistrationEndDate;
            sheet.Cell(row, 18).Value = listing.StartDate;
            sheet.Cell(row, 19).Value = listing.EndDate;
            sheet.Cell(row, 20).Value = listing.CreatedAt;
            row++;
        }
    }

    private static void WriteOrdersSheet(
        XLWorkbook workbook,
        IReadOnlyList<DashboardExportOrderRow> orders)
    {
        var sheet = workbook.Worksheets.Add("Orders");
        WriteHeader(sheet, 1,
        [
            "Order Id", "Order Reference", "Buyer Id", "Buyer Name", "Status", "Order Source",
            "Subtotal", "Shipping Fee", "Vault Insurance", "Platform Fee", "Seller Fee",
            "Seller Proceeds", "Deposit Applied", "Total Amount", "Payment Method", "Created At (UTC)"
        ]);

        var row = 2;
        if (orders.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No orders in selected period";
            return;
        }

        foreach (var order in orders)
        {
            sheet.Cell(row, 1).Value = order.OrderId;
            sheet.Cell(row, 2).Value = order.OrderReference;
            sheet.Cell(row, 3).Value = order.BuyerId;
            sheet.Cell(row, 4).Value = order.BuyerName;
            sheet.Cell(row, 5).Value = order.Status;
            sheet.Cell(row, 6).Value = order.OrderSource;
            sheet.Cell(row, 7).Value = order.Subtotal;
            sheet.Cell(row, 8).Value = order.ShippingFee;
            sheet.Cell(row, 9).Value = order.VaultInsurance;
            sheet.Cell(row, 10).Value = order.PlatformFee;
            sheet.Cell(row, 11).Value = order.SellerFee;
            sheet.Cell(row, 12).Value = order.SellerProceeds;
            sheet.Cell(row, 13).Value = order.DepositApplied;
            sheet.Cell(row, 14).Value = order.TotalAmount;
            sheet.Cell(row, 15).Value = order.PaymentMethod;
            sheet.Cell(row, 16).Value = order.CreatedAt;
            row++;
        }
    }

    private static void WritePaymentsSheet(
        XLWorkbook workbook,
        IReadOnlyList<DashboardExportPaymentRow> payments)
    {
        var sheet = workbook.Worksheets.Add("Payments");
        WriteHeader(sheet, 1,
        [
            "Payment Id", "Order Id", "Order Reference", "Buyer Id", "Buyer Name",
            "Amount", "Status", "Transaction Id", "PayPal Order Id", "Paid At (UTC)", "Created At (UTC)"
        ]);

        var row = 2;
        if (payments.Count == 0)
        {
            sheet.Cell(row, 1).Value = "No successful payments in selected period";
            return;
        }

        foreach (var payment in payments)
        {
            sheet.Cell(row, 1).Value = payment.PaymentId;
            sheet.Cell(row, 2).Value = payment.OrderId;
            sheet.Cell(row, 3).Value = payment.OrderReference;
            sheet.Cell(row, 4).Value = payment.BuyerId;
            sheet.Cell(row, 5).Value = payment.BuyerName;
            sheet.Cell(row, 6).Value = payment.Amount;
            sheet.Cell(row, 7).Value = payment.Status;
            sheet.Cell(row, 8).Value = payment.TransactionId;
            sheet.Cell(row, 9).Value = payment.PayPalOrderId;
            sheet.Cell(row, 10).Value = payment.PaidAt;
            sheet.Cell(row, 11).Value = payment.CreatedAt;
            row++;
        }
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
        }
    }

    private async Task<IReadOnlyList<DashboardExportStatusRow>> GetAuctionStatusBreakdownForExportAsync(
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null)
            .GroupBy(auction => auction.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Status)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(item => new DashboardExportStatusRow
            {
                Status = item.Status,
                StatusLabel = FormatStatusLabel(item.Status),
                Count = item.Count,
                Bucket = ResolveStatusBucket(item.Status)
            })
            .ToList();
    }

    private static string ResolveStatusBucket(string status)
    {
        if (AuctionStatuses.IsConfirming(status))
        {
            return "Pending Verification";
        }

        if (OngoingAuctionStatuses.Contains(status))
        {
            return "Ongoing";
        }

        if (EndedAuctionStatuses.Contains(status))
        {
            return "Ended";
        }

        if (CancelledAuctionStatuses.Contains(status))
        {
            return "Cancelled";
        }

        return "Other";
    }

    private async Task<IReadOnlyList<DashboardCategoryBreakdownViewModel>> GetFullCategoryBidBreakdownForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var grouped = await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.Auction.DeletedAt == null
                          && bid.Auction.Product.DeletedAt == null
                          && bid.PlacedAt >= rangeStart
                          && bid.PlacedAt < rangeEndExclusive)
            .GroupBy(bid => new
            {
                bid.Auction.Product.CategoryId,
                bid.Auction.Product.Category.Name
            })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                BidCount = group.Count(),
                BidVolume = group.Sum(bid => bid.Amount)
            })
            .OrderByDescending(item => item.BidVolume)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return [];
        }

        var totalVolume = grouped.Sum(item => item.BidVolume);
        return grouped
            .Select(item => new DashboardCategoryBreakdownViewModel
            {
                CategoryId = item.CategoryId,
                CategoryName = item.Name,
                BidCount = item.BidCount,
                BidVolume = item.BidVolume,
                Percentage = totalVolume == 0
                    ? 0
                    : Math.Round(item.BidVolume / totalVolume * 100m, 1)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardExportListingRow>> GetListingsInRangeForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var listings = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null
                              && auction.Product.DeletedAt == null
                              && auction.CreatedAt >= rangeStart
                              && auction.CreatedAt < rangeEndExclusive)
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => new
            {
                auction.Id,
                auction.ProductId,
                ProductName = auction.Product.Name,
                auction.ListingType,
                auction.Status,
                CategoryName = auction.Product.Category.Name,
                SellerId = auction.Product.SellerId,
                SellerName = auction.Product.Seller.FullName,
                auction.StartingPrice,
                auction.CurrentPrice,
                auction.BuyNowPrice,
                BidCount = auction.Bids.Count(bid => bid.DeletedAt == null),
                RegistrationCount = auction.Registrations.Count(registration => registration.DeletedAt == null),
                auction.WinnerId,
                auction.RegistrationStartDate,
                auction.RegistrationEndDate,
                auction.StartDate,
                auction.EndDate,
                auction.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return listings
            .Select(listing => new DashboardExportListingRow
            {
                ListingId = listing.Id,
                ProductId = listing.ProductId,
                ProductName = listing.ProductName,
                ListingType = listing.ListingType,
                Status = listing.Status,
                StatusLabel = FormatStatusLabel(listing.Status),
                CategoryName = listing.CategoryName,
                SellerId = listing.SellerId,
                SellerName = listing.SellerName,
                StartingPrice = listing.StartingPrice,
                CurrentPrice = listing.CurrentPrice,
                BuyNowPrice = listing.BuyNowPrice,
                BidCount = listing.BidCount,
                RegistrationCount = listing.RegistrationCount,
                WinnerId = listing.WinnerId,
                RegistrationStartDate = listing.RegistrationStartDate,
                RegistrationEndDate = listing.RegistrationEndDate,
                StartDate = listing.StartDate,
                EndDate = listing.EndDate,
                CreatedAt = listing.CreatedAt
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardExportOrderRow>> GetOrdersInRangeForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return await _dbContext.Orders.AsNoTracking()
            .Where(order => order.DeletedAt == null
                            && order.CreatedAt >= rangeStart
                            && order.CreatedAt < rangeEndExclusive)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new DashboardExportOrderRow
            {
                OrderId = order.Id,
                OrderReference = order.OrderReference,
                BuyerId = order.BuyerId,
                BuyerName = order.Buyer.FullName,
                Status = order.Status,
                OrderSource = order.OrderSource,
                Subtotal = order.Subtotal,
                ShippingFee = order.ShippingFee,
                VaultInsurance = order.VaultInsurance,
                PlatformFee = order.PlatformFee,
                SellerFee = order.SellerFee,
                SellerProceeds = order.SellerProceeds,
                DepositApplied = order.DepositApplied,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                CreatedAt = order.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DashboardExportPaymentRow>> GetPaymentsInRangeForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt != null
                              && payment.PaidAt >= rangeStart
                              && payment.PaidAt < rangeEndExclusive)
            .OrderByDescending(payment => payment.PaidAt)
            .Select(payment => new DashboardExportPaymentRow
            {
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                OrderReference = payment.Order.OrderReference,
                BuyerId = payment.Order.BuyerId,
                BuyerName = payment.Order.Buyer.FullName,
                Amount = payment.Amount,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                PayPalOrderId = payment.PayPalOrderId,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DashboardExportDailyRevenueRow>> GetDailyRevenueForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var payments = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt != null
                              && payment.PaidAt >= rangeStart
                              && payment.PaidAt < rangeEndExclusive)
            .Select(payment => new { PaidAt = payment.PaidAt!.Value, payment.Amount })
            .ToListAsync(cancellationToken);

        return payments
            .GroupBy(payment => payment.PaidAt.Date)
            .OrderBy(group => group.Key)
            .Select(group => new DashboardExportDailyRevenueRow
            {
                Date = group.Key,
                PaymentCount = group.Count(),
                Amount = group.Sum(item => item.Amount)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardExportUserRow>> GetNewUsersInRangeForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return await _dbContext.Users.AsNoTracking()
            .Where(user => user.DeletedAt == null
                           && user.CreatedAt >= rangeStart
                           && user.CreatedAt < rangeEndExclusive)
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => new DashboardExportUserRow
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private sealed class DashboardExportStatusRow
    {
        public string Status { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public int Count { get; init; }
        public string Bucket { get; init; } = string.Empty;
    }

    private sealed class DashboardExportListingRow
    {
        public int ListingId { get; init; }
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string ListingType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public int SellerId { get; init; }
        public string SellerName { get; init; } = string.Empty;
        public decimal StartingPrice { get; init; }
        public decimal CurrentPrice { get; init; }
        public decimal? BuyNowPrice { get; init; }
        public int BidCount { get; init; }
        public int RegistrationCount { get; init; }
        public int? WinnerId { get; init; }
        public DateTime? RegistrationStartDate { get; init; }
        public DateTime? RegistrationEndDate { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    private sealed class DashboardExportOrderRow
    {
        public int OrderId { get; init; }
        public string OrderReference { get; init; } = string.Empty;
        public int BuyerId { get; init; }
        public string BuyerName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string OrderSource { get; init; } = string.Empty;
        public decimal Subtotal { get; init; }
        public decimal ShippingFee { get; init; }
        public decimal VaultInsurance { get; init; }
        public decimal PlatformFee { get; init; }
        public decimal SellerFee { get; init; }
        public decimal SellerProceeds { get; init; }
        public decimal DepositApplied { get; init; }
        public decimal TotalAmount { get; init; }
        public string? PaymentMethod { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    private sealed class DashboardExportPaymentRow
    {
        public int PaymentId { get; init; }
        public int OrderId { get; init; }
        public string OrderReference { get; init; } = string.Empty;
        public int BuyerId { get; init; }
        public string BuyerName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? TransactionId { get; init; }
        public string? PayPalOrderId { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    private sealed class DashboardExportDailyRevenueRow
    {
        public DateTime Date { get; init; }
        public int PaymentCount { get; init; }
        public decimal Amount { get; init; }
    }

    private sealed class DashboardExportUserRow
    {
        public int UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    private async Task<DashboardRevenueSectionViewModel> BuildRevenueSectionAsync(
        DashboardFilterViewModel filter,
        DashboardFilterViewModel previousRange,
        CancellationToken cancellationToken)
    {
        var gmvCurrent = await SumGmvAsync(filter, cancellationToken);
        var gmvPrevious = await SumGmvAsync(previousRange, cancellationToken);

        var commissionCurrent = await SumCommissionAsync(filter, cancellationToken);
        var commissionPrevious = await SumCommissionAsync(previousRange, cancellationToken);

        var buyerFeeCurrent = await SumBuyerCheckoutFeesAsync(filter, cancellationToken);
        var buyerFeePrevious = await SumBuyerCheckoutFeesAsync(previousRange, cancellationToken);

        var sellerFeeCurrent = await SumSellerSuccessFeesAsync(filter, cancellationToken);
        var sellerFeePrevious = await SumSellerSuccessFeesAsync(previousRange, cancellationToken);

        var sellerProceedsCurrent = await SumSellerProceedsAsync(filter, cancellationToken);
        var sellerProceedsPrevious = await SumSellerProceedsAsync(previousRange, cancellationToken);

        return new DashboardRevenueSectionViewModel
        {
            GmvKpi = BuildKpiCard(
                "GMV",
                FormatCurrency(gmvCurrent),
                gmvCurrent,
                gmvPrevious,
                cardKey: DashboardRevenueCardKeys.Gmv),
            CommissionKpi = BuildKpiCard(
                "Commission",
                FormatCurrency(commissionCurrent),
                commissionCurrent,
                commissionPrevious,
                cardKey: DashboardRevenueCardKeys.Commission),
            BuyerFeeKpi = BuildKpiCard(
                "BuyerFee",
                FormatCurrency(buyerFeeCurrent),
                buyerFeeCurrent,
                buyerFeePrevious,
                cardKey: DashboardRevenueCardKeys.BuyerFee),
            SellerFeeKpi = BuildKpiCard(
                "SellerFee",
                FormatCurrency(sellerFeeCurrent),
                sellerFeeCurrent,
                sellerFeePrevious,
                cardKey: DashboardRevenueCardKeys.SellerFee),
            SellerProceedsKpi = BuildKpiCard(
                "SellerProceeds",
                FormatCurrency(sellerProceedsCurrent),
                sellerProceedsCurrent,
                sellerProceedsPrevious,
                cardKey: DashboardRevenueCardKeys.SellerProceeds)
        };
    }

    private async Task<decimal> SumPaidOrderPlatformFeesAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var fromPayments = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.PlatformFee, cancellationToken);

        var fromOrphanOrders = await OrphanPaidOrdersQuery(startInclusive, endExclusive)
            .SumAsync(order => order.PlatformFee, cancellationToken);

        return fromPayments + fromOrphanOrders;
    }

    private async Task<decimal> SumPaidOrderSellerFeesAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var fromPayments = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.SellerFee, cancellationToken);

        var fromOrphanOrders = await OrphanPaidOrdersQuery(startInclusive, endExclusive)
            .SumAsync(order => order.SellerFee, cancellationToken);

        return fromPayments + fromOrphanOrders;
    }

    private async Task<decimal> SumPaidOrderSellerProceedsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var fromPayments = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.SellerProceeds, cancellationToken);

        var fromOrphanOrders = await OrphanPaidOrdersQuery(startInclusive, endExclusive)
            .SumAsync(order => order.SellerProceeds, cancellationToken);

        return fromPayments + fromOrphanOrders;
    }

    /// <summary>
    /// Paid/delivered orders in period that have no successful payment row
    /// (COD / legacy gap). Avoids double-counting when a success payment exists.
    /// </summary>
    private IQueryable<AuctionOrder> OrphanPaidOrdersQuery(DateTime startInclusive, DateTime endExclusive)
    {
        return _dbContext.Orders.AsNoTracking()
            .Where(order => order.DeletedAt == null
                            && PaidOrderStatuses.Contains(order.Status)
                            && order.CreatedAt >= startInclusive
                            && order.CreatedAt < endExclusive
                            && !_dbContext.Payments.Any(payment =>
                                payment.OrderId == order.Id
                                && payment.DeletedAt == null
                                && payment.Status == PaymentStatuses.Success));
    }

    private Task<decimal> SumOrphanPaidOrderTotalsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return OrphanPaidOrdersQuery(startInclusive, endExclusive)
            .SumAsync(order => order.TotalAmount, cancellationToken);
    }

    private async Task<List<DateTime>> GetRegistrationDatesAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return await _dbContext.Users.AsNoTracking()
            .Where(user => user.DeletedAt == null
                           && user.Status == UserStatus.Active
                           && user.CreatedAt >= rangeStart
                           && user.CreatedAt < rangeEndExclusive)
            .Select(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private static List<DashboardRegistrationChartPointViewModel> BuildRegistrationSeries(
        IReadOnlyList<DateTime> registrationDates,
        string granularity,
        DashboardFilterViewModel filter)
    {
        if (registrationDates.Count == 0)
        {
            return [];
        }

        var grouped = registrationDates
            .GroupBy(date => GetRegistrationBucket(date, granularity))
            .ToDictionary(group => group.Key, group => group.Count());

        var buckets = BuildRegistrationBuckets(filter.DateFrom, filter.DateTo, granularity).ToList();
        var series = new List<DashboardRegistrationChartPointViewModel>(buckets.Count);

        foreach (var bucket in buckets)
        {
            grouped.TryGetValue(bucket.Key, out var count);

            series.Add(new DashboardRegistrationChartPointViewModel
            {
                Label = bucket.Label,
                Value = count,
                FilterKey = bucket.FilterKey
            });
        }

        return series;
    }

    private static IEnumerable<(string Key, string Label, string FilterKey)> BuildRegistrationBuckets(
        DateTime dateFrom,
        DateTime dateTo,
        string granularity)
    {
        if (granularity == "month")
        {
            var cursor = new DateTime(dateFrom.Year, dateFrom.Month, 1);
            var end = new DateTime(dateTo.Year, dateTo.Month, 1);

            while (cursor <= end)
            {
                var key = $"{cursor:yyyy-MM}";
                yield return (key, cursor.ToString("MMM yyyy", CultureInfo.InvariantCulture), cursor.ToString("yyyy-MM-dd"));
                cursor = cursor.AddMonths(1);
            }

            yield break;
        }

        if (granularity == "week")
        {
            var cursor = dateFrom.Date;
            while (cursor <= dateTo.Date)
            {
                var key = $"{ISOWeek.GetYear(cursor):0000}-W{ISOWeek.GetWeekOfYear(cursor):00}";
                var weekStart = ISOWeek.ToDateTime(ISOWeek.GetYear(cursor), ISOWeek.GetWeekOfYear(cursor), DayOfWeek.Monday);
                yield return (key, $"W{ISOWeek.GetWeekOfYear(cursor):00}", weekStart.ToString("yyyy-MM-dd"));
                cursor = cursor.AddDays(7);
            }

            yield break;
        }

        for (var cursor = dateFrom.Date; cursor <= dateTo.Date; cursor = cursor.AddDays(1))
        {
            var key = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            yield return (key, cursor.ToString("MMM d", CultureInfo.InvariantCulture), key);
        }
    }

    private static string GetRegistrationBucket(DateTime createdAt, string granularity)
    {
        var date = createdAt.Date;

        return granularity switch
        {
            "week" => $"{ISOWeek.GetYear(date):0000}-W{ISOWeek.GetWeekOfYear(date):00}",
            "month" => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            _ => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }

    private static DashboardFilterViewModel BuildPreviousRange(DashboardFilterViewModel filter)
    {
        var periodDays = filter.PeriodDays;
        var previousEnd = filter.DateFrom.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(periodDays - 1));

        return new DashboardFilterViewModel
        {
            DateFrom = previousStart,
            DateTo = previousEnd
        };
    }

    private async Task<decimal> SumSuccessfulPaymentsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Order.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive)
            .SumAsync(payment => payment.Amount, cancellationToken);
    }

    private static DashboardKpiCardViewModel BuildSnapshotKpi(
        string label,
        string displayValue,
        decimal numericValue = 0,
        string? cardKey = null)
    {
        return new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
            NumericValue = numericValue,
            CardKey = cardKey,
            ChangeDisplay = string.Empty
        };
    }

    private static DashboardKpiCardViewModel BuildKpiCard(
        string label,
        string displayValue,
        decimal currentValue,
        decimal previousValue,
        string? cardKey = null)
    {
        var card = new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
            NumericValue = currentValue,
            CardKey = cardKey
        };

        var changePercent = CalculateChangePercent(currentValue, previousValue);
        card.ChangePercent = changePercent;
        card.IsPositiveChange = changePercent >= 0;
        card.ChangeDisplay = $"{(changePercent >= 0 ? "+" : string.Empty)}{changePercent:0.#}%";
        return card;
    }

    private static decimal CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            // No prior baseline: flat zero stays 0%, otherwise treat growth from zero as +100%.
            return current == 0 ? 0m : 100m;
        }

        return Math.Round((current - previous) / previous * 100m, 1);
    }

    private static string FormatInteger(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatCurrency(decimal value) => value.ToString("$#,##0.00", CultureInfo.InvariantCulture);

    private static string FormatStatusLabel(string status) =>
        string.IsNullOrWhiteSpace(status)
            ? "Unknown"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace('_', ' '));
}
