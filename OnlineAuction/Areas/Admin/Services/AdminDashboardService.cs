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

    public Task<decimal> SumGmvAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        return SumSuccessfulPaymentsAsync(rangeStart, rangeEndExclusive, cancellationToken);
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
        var activeUsersCount = await GetActiveUsersCountAsync(filter, cancellationToken);

        var registrationDates = await GetRegistrationDatesAsync(filter, cancellationToken);
        var statusCounts = await GetAuctionStatusCountsAsync(cancellationToken);
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
                ActiveUsersKpi = BuildSnapshotKpi("Active Users", FormatInteger(activeUsersCount)),
                RegistrationByDay = BuildRegistrationSeries(registrationDates, "day", filter),
                RegistrationByWeek = BuildRegistrationSeries(registrationDates, "week", filter),
                RegistrationByMonth = BuildRegistrationSeries(registrationDates, "month", filter),
                TopBuyers = await GetTopBuyersAsync(filter, cancellationToken),
                TopSellers = await GetTopSellersAsync(filter, cancellationToken)
            },
            AuctionSection = new DashboardAuctionSectionViewModel
            {
                OngoingKpi = BuildSnapshotKpi("Ongoing Auctions", FormatInteger(statusCounts.Ongoing)),
                EndedKpi = BuildSnapshotKpi("Ended Auctions", FormatInteger(statusCounts.Ended)),
                CancelledKpi = BuildSnapshotKpi("Cancelled Auctions", FormatInteger(statusCounts.Cancelled)),
                SuccessRateKpi = BuildSnapshotKpi(
                    "Success Rate",
                    successRate.HasValue ? $"{successRate.Value:0.0}%" : "N/A"),
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
                          && bid.PlacedAt >= rangeStart
                          && bid.PlacedAt < rangeEndExclusive)
            .Select(bid => bid.BidderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var buyerIds = await _dbContext.Orders.AsNoTracking()
            .Where(order => order.DeletedAt == null
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
            .Where(user => sellerIds.Contains(user.Id))
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

    public async Task<decimal?> GetAuctionSuccessRateAsync(CancellationToken cancellationToken = default)
    {
        var denominator = await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && SuccessRateDenominatorStatuses.Contains(auction.Status),
                cancellationToken);

        if (denominator == 0)
        {
            return null;
        }

        var numerator = await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
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
        var auctionsInRange = await GetAuctionsInRangeForExportAsync(filter, cancellationToken);
        var generatedAtUtc = DateTime.UtcNow;

        using var workbook = new XLWorkbook();

        var overviewSheet = workbook.Worksheets.Add("Overview");
        overviewSheet.Cell(1, 1).Value = "Field";
        overviewSheet.Cell(1, 2).Value = "Value";
        overviewSheet.Cell(2, 1).Value = "Date From";
        overviewSheet.Cell(2, 2).Value = filter.DateFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        overviewSheet.Cell(3, 1).Value = "Date To";
        overviewSheet.Cell(3, 2).Value = filter.DateTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        overviewSheet.Cell(4, 1).Value = "Report Generated (UTC)";
        overviewSheet.Cell(4, 2).Value = generatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        var overviewMetricRow = 6;
        overviewSheet.Cell(overviewMetricRow, 1).Value = "Metric";
        overviewSheet.Cell(overviewMetricRow, 2).Value = "Value";
        overviewMetricRow++;

        void AddOverviewMetric(string metric, string value)
        {
            overviewSheet.Cell(overviewMetricRow, 1).Value = metric;
            overviewSheet.Cell(overviewMetricRow, 2).Value = value;
            overviewMetricRow++;
        }

        AddOverviewMetric("GMV", dashboard.RevenueSection.GmvKpi.DisplayValue);
        AddOverviewMetric("Commission", dashboard.RevenueSection.CommissionKpi.DisplayValue);
        AddOverviewMetric("New Registrations", dashboard.UserSection.NewRegistrationsKpi.DisplayValue);
        AddOverviewMetric("Active Users", dashboard.UserSection.ActiveUsersKpi.DisplayValue);
        AddOverviewMetric("Ongoing Auctions", dashboard.AuctionSection.OngoingKpi.DisplayValue);
        AddOverviewMetric("Ended Auctions", dashboard.AuctionSection.EndedKpi.DisplayValue);
        AddOverviewMetric("Cancelled Auctions", dashboard.AuctionSection.CancelledKpi.DisplayValue);
        AddOverviewMetric("Success Rate", dashboard.AuctionSection.SuccessRateKpi.DisplayValue);

        var revenueSheet = workbook.Worksheets.Add("Revenue");
        revenueSheet.Cell(1, 1).Value = "Metric";
        revenueSheet.Cell(1, 2).Value = "Value";
        revenueSheet.Cell(2, 1).Value = "GMV";
        revenueSheet.Cell(2, 2).Value = dashboard.RevenueSection.GmvKpi.DisplayValue;
        revenueSheet.Cell(3, 1).Value = "Commission";
        revenueSheet.Cell(3, 2).Value = dashboard.RevenueSection.CommissionKpi.DisplayValue;
        revenueSheet.Cell(4, 1).Value = "BuyerCheckoutFee";
        revenueSheet.Cell(4, 2).Value = dashboard.RevenueSection.BuyerFeeKpi.DisplayValue;
        revenueSheet.Cell(5, 1).Value = "SellerSuccessFee";
        revenueSheet.Cell(5, 2).Value = dashboard.RevenueSection.SellerFeeKpi.DisplayValue;
        revenueSheet.Cell(6, 1).Value = "SellerProceeds";
        revenueSheet.Cell(6, 2).Value = dashboard.RevenueSection.SellerProceedsKpi.DisplayValue;

        var auctionsSheet = workbook.Worksheets.Add("Auctions");
        auctionsSheet.Cell(1, 1).Value = "Metric";
        auctionsSheet.Cell(1, 2).Value = "Value";
        auctionsSheet.Cell(2, 1).Value = "Ongoing Auctions";
        auctionsSheet.Cell(2, 2).Value = dashboard.AuctionSection.OngoingKpi.DisplayValue;
        auctionsSheet.Cell(3, 1).Value = "Ended Auctions";
        auctionsSheet.Cell(3, 2).Value = dashboard.AuctionSection.EndedKpi.DisplayValue;
        auctionsSheet.Cell(4, 1).Value = "Cancelled Auctions";
        auctionsSheet.Cell(4, 2).Value = dashboard.AuctionSection.CancelledKpi.DisplayValue;
        auctionsSheet.Cell(5, 1).Value = "Success Rate";
        auctionsSheet.Cell(5, 2).Value = dashboard.AuctionSection.SuccessRateKpi.DisplayValue;

        var categoryHeaderRow = 7;
        auctionsSheet.Cell(categoryHeaderRow, 1).Value = "Category Breakdown";
        auctionsSheet.Cell(categoryHeaderRow + 1, 1).Value = "Category";
        auctionsSheet.Cell(categoryHeaderRow + 1, 2).Value = "Bid Count";
        auctionsSheet.Cell(categoryHeaderRow + 1, 3).Value = "Bid Volume";
        auctionsSheet.Cell(categoryHeaderRow + 1, 4).Value = "Percentage";

        var categoryRow = categoryHeaderRow + 2;
        if (dashboard.AuctionSection.CategoryBreakdown.Count == 0)
        {
            auctionsSheet.Cell(categoryRow, 1).Value = "No data for selected period";
            categoryRow++;
        }
        else
        {
            foreach (var category in dashboard.AuctionSection.CategoryBreakdown)
            {
                auctionsSheet.Cell(categoryRow, 1).Value = category.CategoryName;
                auctionsSheet.Cell(categoryRow, 2).Value = category.BidCount;
                auctionsSheet.Cell(categoryRow, 3).Value = category.BidVolume;
                auctionsSheet.Cell(categoryRow, 4).Value = category.Percentage;
                categoryRow++;
            }
        }

        var auctionListHeaderRow = categoryRow + 1;
        auctionsSheet.Cell(auctionListHeaderRow, 1).Value = "Auctions In Range";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 1).Value = "Product";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 2).Value = "Seller";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 3).Value = "Category";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 4).Value = "Current Bid";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 5).Value = "Status";
        auctionsSheet.Cell(auctionListHeaderRow + 1, 6).Value = "Created At";

        var auctionRow = auctionListHeaderRow + 2;
        if (auctionsInRange.Count == 0)
        {
            auctionsSheet.Cell(auctionRow, 1).Value = "No data for selected period";
        }
        else
        {
            foreach (var auction in auctionsInRange)
            {
                auctionsSheet.Cell(auctionRow, 1).Value = auction.ProductName;
                auctionsSheet.Cell(auctionRow, 2).Value = auction.SellerName;
                auctionsSheet.Cell(auctionRow, 3).Value = auction.CategoryName;
                auctionsSheet.Cell(auctionRow, 4).Value = auction.CurrentPrice;
                auctionsSheet.Cell(auctionRow, 5).Value = auction.StatusLabel;
                auctionsSheet.Cell(auctionRow, 6).Value = auction.CreatedAt;
                auctionRow++;
            }
        }

        overviewSheet.Columns().AdjustToContents();
        revenueSheet.Columns().AdjustToContents();
        auctionsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportSummaryCsvAsync(int periodDays = 7, CancellationToken cancellationToken = default)
    {
        var filter = NormalizeFilter(
            DateTime.UtcNow.Date.AddDays(-(periodDays - 1)),
            DateTime.UtcNow.Date);

        return await ExportExcelAsync(filter, cancellationToken);
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
        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.PlatformFee, cancellationToken);
    }

    private async Task<decimal> SumPaidOrderSellerFeesAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.SellerFee, cancellationToken);
    }

    private async Task<decimal> SumPaidOrderSellerProceedsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .SumAsync(payment => payment.Order.SellerProceeds, cancellationToken);
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

    private async Task<IReadOnlyList<DashboardExportAuctionRow>> GetAuctionsInRangeForExportAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var auctions = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null
                              && auction.Product.DeletedAt == null
                              && auction.CreatedAt >= rangeStart
                              && auction.CreatedAt < rangeEndExclusive)
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => new
            {
                auction.Product.Name,
                SellerName = auction.Product.Seller.FullName,
                CategoryName = auction.Product.Category.Name,
                auction.CurrentPrice,
                auction.Status,
                auction.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return auctions
            .Select(auction => new DashboardExportAuctionRow
            {
                ProductName = auction.Name,
                SellerName = auction.SellerName,
                CategoryName = auction.CategoryName,
                CurrentPrice = auction.CurrentPrice,
                StatusLabel = FormatStatusLabel(auction.Status),
                CreatedAt = auction.CreatedAt
            })
            .ToList();
    }

    private sealed class DashboardExportAuctionRow
    {
        public string ProductName { get; init; } = string.Empty;

        public string SellerName { get; init; } = string.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public decimal CurrentPrice { get; init; }

        public string StatusLabel { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }
    }

    private async Task<decimal> SumSuccessfulPaymentsAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startInclusive
                              && payment.PaidAt < endExclusive)
            .SumAsync(payment => payment.Amount, cancellationToken);
    }

    private static DashboardKpiCardViewModel BuildSnapshotKpi(
        string label,
        string displayValue)
    {
        return new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
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
            CardKey = cardKey
        };

        var changePercent = CalculateChangePercent(currentValue, previousValue);
        card.ChangePercent = changePercent;

        if (!changePercent.HasValue)
        {
            card.ChangeDisplay = "N/A";
            card.IsPositiveChange = true;
            return card;
        }

        card.IsPositiveChange = changePercent.Value >= 0;
        card.ChangeDisplay = $"{(changePercent.Value >= 0 ? "+" : string.Empty)}{changePercent.Value:0.#}%";
        return card;
    }

    private static decimal? CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return null;
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
