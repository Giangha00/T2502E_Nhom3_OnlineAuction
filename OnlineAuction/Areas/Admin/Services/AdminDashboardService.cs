using System.Globalization;
using System.Text;
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
    private const int RecentAuctionCount = 10;
    private const int TopUserCount = 10;
    private const int CategoryTopCount = 5;
    private const int DefaultFilterDays = 30;

    private static readonly string[] ActiveAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

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
        string? statusFilter = null,
        int? categoryIdFilter = null,
        DateTime? registrationDateFilter = null,
        string? registrationGranularity = null,
        string? sectionFilter = null,
        string? revenueTypeFilter = null)
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

        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

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
            StatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter.Trim(),
            CategoryIdFilter = categoryIdFilter,
            RegistrationDateFilter = registrationDateFilter?.Date,
            RegistrationGranularity = granularity,
            SectionFilter = string.IsNullOrWhiteSpace(sectionFilter) ? null : sectionFilter.Trim(),
            RevenueTypeFilter = NormalizeRevenueTypeFilter(revenueTypeFilter)
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

    public async Task<decimal> SumPlatformRevenueAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var registrationDeposits = await SumRegistrationDepositRevenueAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);
        var buyerCheckoutFees = await SumPaidOrderPlatformFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);
        var sellerSuccessFees = await SumPaidOrderSellerFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);

        return registrationDeposits + buyerCheckoutFees + sellerSuccessFees;
    }

    public async Task<IReadOnlyList<DashboardRevenueDetailViewModel>> BuildRevenueDetailTableAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);
        var rows = new List<DashboardRevenueDetailViewModel>();

        var paymentRows = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= rangeStart
                              && payment.PaidAt < rangeEndExclusive)
            .Select(payment => new
            {
                payment.PaidAt,
                payment.Amount,
                payment.OrderId,
                payment.Order.OrderReference,
                AuctionId = payment.Order.Items
                    .Where(item => item.DeletedAt == null)
                    .Select(item => (int?)item.AuctionId)
                    .FirstOrDefault(),
                PlatformFee = payment.Order.PlatformFee
            })
            .ToListAsync(cancellationToken);

        rows.AddRange(paymentRows.Select(row => new DashboardRevenueDetailViewModel
        {
            TransactionDate = row.PaidAt!.Value,
            Type = DashboardRevenueTypes.OrderPayment,
            ReferenceCode = row.OrderReference,
            AuctionId = row.AuctionId,
            OrderId = row.OrderId,
            GmvAmount = row.Amount,
            PlatformRevenueAmount = row.PlatformFee,
            Status = PaymentStatuses.Success
        }));

        var sellerFeeRows = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= rangeStart
                              && payment.PaidAt < rangeEndExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status)
                              && payment.Order.SellerFee > 0)
            .Select(payment => new
            {
                payment.PaidAt,
                payment.OrderId,
                payment.Order.OrderReference,
                payment.Order.SellerFee,
                AuctionId = payment.Order.Items
                    .Where(item => item.DeletedAt == null)
                    .Select(item => (int?)item.AuctionId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        rows.AddRange(sellerFeeRows.Select(row => new DashboardRevenueDetailViewModel
        {
            TransactionDate = row.PaidAt!.Value,
            Type = DashboardRevenueTypes.SellerSuccessFee,
            ReferenceCode = row.OrderReference,
            AuctionId = row.AuctionId,
            OrderId = row.OrderId,
            GmvAmount = 0m,
            PlatformRevenueAmount = row.SellerFee,
            Status = PaymentStatuses.Success
        }));

        var depositRows = await _dbContext.AuctionRegistrationDeposits.AsNoTracking()
            .Where(deposit => deposit.DeletedAt == null
                              && deposit.PaidAt >= rangeStart
                              && deposit.PaidAt < rangeEndExclusive
                              && (deposit.Status == AuctionRegistrationDepositStatuses.Paid
                                  || deposit.Status == AuctionRegistrationDepositStatuses.Applied))
            .Select(deposit => new
            {
                deposit.PaidAt,
                deposit.AuctionId,
                deposit.Amount,
                deposit.Status
            })
            .ToListAsync(cancellationToken);

        rows.AddRange(depositRows.Select(row => new DashboardRevenueDetailViewModel
        {
            TransactionDate = row.PaidAt!.Value,
            Type = DashboardRevenueTypes.RegistrationDeposit,
            ReferenceCode = $"DEP-{row.AuctionId}",
            AuctionId = row.AuctionId,
            GmvAmount = 0m,
            PlatformRevenueAmount = row.Amount,
            Status = row.Status
        }));

        var filtered = ApplyRevenueTypeFilter(rows, filter.RevenueTypeFilter);

        return filtered
            .OrderByDescending(row => row.TransactionDate)
            .ToList();
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(
        DashboardFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);
        var previousRange = BuildPreviousRange(filter);

        var activeAuctionsNow = await CountActiveAuctionsAsync(cancellationToken);
        var activeAuctionsPrevious = await CountActiveAuctionsAtAsync(
            utcNow.Date.AddDays(-filter.PeriodDays),
            cancellationToken);

        var newRegistrationsCurrent = await GetNewUserRegistrationsCountAsync(filter, cancellationToken);
        var newRegistrationsPrevious = await GetNewUserRegistrationsCountAsync(previousRange, cancellationToken);
        var totalActiveUsers = await _dbContext.Users.AsNoTracking()
            .CountAsync(
                user => user.DeletedAt == null && user.Status == UserStatus.Active,
                cancellationToken);

        var activeUsersCount = await GetActiveUsersCountAsync(filter, cancellationToken);

        var bidsInRange = await _dbContext.Bids.AsNoTracking()
            .CountAsync(
                bid => bid.DeletedAt == null
                       && bid.PlacedAt >= rangeStart
                       && bid.PlacedAt < rangeEndExclusive,
                cancellationToken);

        var bidsPreviousRange = await _dbContext.Bids.AsNoTracking()
            .CountAsync(
                bid => bid.DeletedAt == null
                       && bid.PlacedAt >= previousRange.DateFrom
                       && bid.PlacedAt < previousRange.DateTo.AddDays(1),
                cancellationToken);

        var revenueCurrentPeriod = await SumGmvAsync(filter, cancellationToken);
        var revenuePreviousPeriod = await SumGmvAsync(previousRange, cancellationToken);

        var pendingPayments = await _dbContext.Orders.AsNoTracking()
            .CountAsync(
                order => order.DeletedAt == null && order.Status == OrderStatuses.PendingPayment,
                cancellationToken);

        var pendingRegistrations = await _dbContext.AuctionRegistrations.AsNoTracking()
            .CountAsync(
                registration => registration.DeletedAt == null
                                && registration.Status == AuctionRegistrationStatuses.Pending,
                cancellationToken);

        var pendingVerifications = await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && auction.Product.DeletedAt == null
                           && auction.Status == AuctionStatuses.PendingReview,
                cancellationToken);

        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var completedOrdersThisMonth = await _dbContext.Orders.AsNoTracking()
            .CountAsync(
                order => order.DeletedAt == null
                         && PaidOrderStatuses.Contains(order.Status)
                         && order.CreatedAt >= monthStart,
                cancellationToken);

        var pendingComplaints = await _dbContext.Complaints.AsNoTracking()
            .CountAsync(
                complaint => complaint.DeletedAt == null && complaint.Status == ComplaintStatuses.Pending,
                cancellationToken);

        var statusCounts = await GetAuctionStatusCountsAsync(cancellationToken);
        var successRate = await GetAuctionSuccessRateAsync(cancellationToken);

        var kpiCards = new List<DashboardKpiCardViewModel>
        {
            BuildKpiCard("Active Auctions", FormatInteger(activeAuctionsNow), activeAuctionsNow, activeAuctionsPrevious),
            BuildKpiCard("Registered Users", FormatInteger(totalActiveUsers), newRegistrationsCurrent, newRegistrationsPrevious),
            BuildKpiCard("Total Bids", FormatInteger(bidsInRange), bidsInRange, bidsPreviousRange),
            BuildKpiCard("GMV", FormatCurrency(revenueCurrentPeriod), revenueCurrentPeriod, revenuePreviousPeriod, cardKey: DashboardRevenueCardKeys.Gmv)
        };

        var secondaryKpiCards = new List<DashboardKpiCardViewModel>
        {
            BuildKpiCard("Pending Verifications", FormatInteger(pendingVerifications), pendingVerifications, 0, includeChange: false, linkUrl: "/Admin/AuctionVerification"),
            BuildKpiCard("Pending Complaints", FormatInteger(pendingComplaints), pendingComplaints, 0, includeChange: false, linkUrl: "/Admin/Complaint?Status=pending"),
            BuildKpiCard("Pending Payments", FormatInteger(pendingPayments), pendingPayments, 0, includeChange: false),
            BuildKpiCard("Pending Registrations", FormatInteger(pendingRegistrations), pendingRegistrations, 0, includeChange: false),
            BuildKpiCard("Completed Orders (Month)", FormatInteger(completedOrdersThisMonth), completedOrdersThisMonth, 0, includeChange: false)
        };

        var registrationDates = await GetRegistrationDatesAsync(filter, cancellationToken);
        var newUsers = await GetNewUsersAsync(filter, cancellationToken);
        var filteredNewUsers = ApplyRegistrationChartFilter(newUsers, filter);

        var userSection = new DashboardUserSectionViewModel
        {
            NewRegistrationsKpi = BuildKpiCard(
                "New Registrations",
                FormatInteger(newRegistrationsCurrent),
                newRegistrationsCurrent,
                newRegistrationsPrevious),
            ActiveUsersKpi = BuildSnapshotKpi("Active Users", FormatInteger(activeUsersCount)),
            TotalUsersKpi = BuildSnapshotKpi("Total Users", FormatInteger(totalActiveUsers)),
            RegistrationByDay = BuildRegistrationSeries(registrationDates, "day", filter),
            RegistrationByWeek = BuildRegistrationSeries(registrationDates, "week", filter),
            RegistrationByMonth = BuildRegistrationSeries(registrationDates, "month", filter),
            TopBuyers = await GetTopBuyersAsync(filter, cancellationToken),
            TopSellers = await GetTopSellersAsync(filter, cancellationToken),
            NewUsers = filteredNewUsers
        };

        var revenueSection = await BuildRevenueSectionAsync(filter, previousRange, cancellationToken);

        var auctionSection = new DashboardAuctionSectionViewModel
        {
            OngoingKpi = BuildSnapshotKpi("Ongoing Auctions", FormatInteger(statusCounts.Ongoing)),
            EndedKpi = BuildSnapshotKpi("Ended Auctions", FormatInteger(statusCounts.Ended)),
            CancelledKpi = BuildSnapshotKpi("Cancelled Auctions", FormatInteger(statusCounts.Cancelled)),
            PendingReviewKpi = BuildSnapshotKpi(
                "Pending Review",
                FormatInteger(statusCounts.PendingReview),
                "/Admin/AuctionVerification"),
            SuccessRateKpi = BuildSnapshotKpi(
                "Success Rate",
                successRate.HasValue ? $"{successRate.Value:0.0}%" : "N/A"),
            CategoryBreakdown = await GetCategoryBidBreakdownAsync(filter, cancellationToken)
        };

        var recentAuctionsQuery = _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.StatusFilter))
        {
            recentAuctionsQuery = recentAuctionsQuery.Where(auction => auction.Status == filter.StatusFilter);
        }

        if (filter.CategoryIdFilter.HasValue)
        {
            recentAuctionsQuery = recentAuctionsQuery.Where(
                auction => auction.Product.CategoryId == filter.CategoryIdFilter.Value);
        }

        var recentAuctions = await recentAuctionsQuery
            .OrderByDescending(auction => auction.CreatedAt)
            .Take(RecentAuctionCount)
            .Select(auction => new
            {
                auction.Id,
                auction.Product.Name,
                SellerName = auction.Product.Seller.FullName,
                CategoryId = auction.Product.CategoryId,
                CategoryName = auction.Product.Category.Name,
                auction.CurrentPrice,
                auction.Status,
                auction.EndDate
            })
            .ToListAsync(cancellationToken);

        var bidsChart = await BuildDailyBidSeriesAsync(rangeStart, rangeEndExclusive, cancellationToken);
        var statusBreakdown = await BuildStatusBreakdownAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            Filter = filter,
            KpiCards = kpiCards,
            SecondaryKpiCards = secondaryKpiCards,
            UserSection = userSection,
            RevenueSection = revenueSection,
            AuctionSection = auctionSection,
            RecentAuctions = recentAuctions
                .Select(auction => new DashboardRecentAuctionViewModel
                {
                    Id = auction.Id,
                    ProductName = auction.Name,
                    SellerName = auction.SellerName,
                    CategoryId = auction.CategoryId,
                    CategoryName = auction.CategoryName,
                    CurrentPrice = auction.CurrentPrice,
                    Status = auction.Status,
                    StatusLabel = FormatStatusLabel(auction.Status),
                    StatusBadgeClass = GetStatusBadgeClass(auction.Status),
                    EndsIn = FormatEndsIn(auction.EndDate, auction.Status, utcNow)
                })
                .ToList(),
            BidsChart = bidsChart,
            AuctionStatusBreakdown = statusBreakdown
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

        var salesTotals = await _dbContext.OrderItems.AsNoTracking()
            .Where(item => item.DeletedAt == null
                           && item.Order.DeletedAt == null
                           && PaidOrderStatuses.Contains(item.Order.Status)
                           && item.Order.CreatedAt >= rangeStart
                           && item.Order.CreatedAt < rangeEndExclusive)
            .GroupBy(item => item.Auction.Product.SellerId)
            .Select(group => new { SellerId = group.Key, TotalSales = group.Sum(item => item.WinningBid) })
            .ToListAsync(cancellationToken);

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

        return sellerIds
            .Select(sellerId => new DashboardTopUserViewModel
            {
                UserId = sellerId,
                FullName = sellerNames.GetValueOrDefault(sellerId, "Unknown"),
                ListingCount = listingLookup.GetValueOrDefault(sellerId),
                TotalSales = salesLookup.GetValueOrDefault(sellerId)
            })
            .OrderByDescending(item => item.TotalSales)
            .ThenByDescending(item => item.ListingCount)
            .Take(TopUserCount)
            .ToList();
    }

    public async Task<(int Ongoing, int Ended, int Cancelled, int PendingReview)> GetAuctionStatusCountsAsync(
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
        var pendingReview = lookup.GetValueOrDefault(AuctionStatuses.PendingReview);

        return (ongoing, ended, cancelled, pendingReview);
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

        using var workbook = new XLWorkbook();

        var overviewSheet = workbook.Worksheets.Add("Overview");
        overviewSheet.Cell(1, 1).Value = "Metric";
        overviewSheet.Cell(1, 2).Value = "Value";
        overviewSheet.Cell(2, 1).Value = "GMV";
        overviewSheet.Cell(2, 2).Value = dashboard.RevenueSection.GmvKpi.DisplayValue;
        overviewSheet.Cell(3, 1).Value = "Platform Revenue";
        overviewSheet.Cell(3, 2).Value = dashboard.RevenueSection.PlatformRevenueKpi.DisplayValue;
        overviewSheet.Cell(4, 1).Value = "Completed Orders";
        overviewSheet.Cell(4, 2).Value = dashboard.RevenueSection.CompletedOrdersKpi.DisplayValue;
        overviewSheet.Cell(5, 1).Value = "Active Auctions";
        overviewSheet.Cell(5, 2).Value = dashboard.KpiCards.FirstOrDefault(card => card.Label == "Active Auctions")?.DisplayValue ?? string.Empty;
        overviewSheet.Cell(6, 1).Value = "Total Bids";
        overviewSheet.Cell(6, 2).Value = dashboard.KpiCards.FirstOrDefault(card => card.Label == "Total Bids")?.DisplayValue ?? string.Empty;

        var usersSheet = workbook.Worksheets.Add("Users");
        usersSheet.Cell(1, 1).Value = "Metric";
        usersSheet.Cell(1, 2).Value = "Value";
        usersSheet.Cell(2, 1).Value = "New Registrations";
        usersSheet.Cell(2, 2).Value = dashboard.UserSection.NewRegistrationsKpi.DisplayValue;
        usersSheet.Cell(3, 1).Value = "Active Users";
        usersSheet.Cell(3, 2).Value = dashboard.UserSection.ActiveUsersKpi.DisplayValue;
        usersSheet.Cell(4, 1).Value = "Total Users";
        usersSheet.Cell(4, 2).Value = dashboard.UserSection.TotalUsersKpi.DisplayValue;

        var buyersStartRow = 6;
        usersSheet.Cell(buyersStartRow, 1).Value = "Top Buyers";
        usersSheet.Cell(buyersStartRow + 1, 1).Value = "Name";
        usersSheet.Cell(buyersStartRow + 1, 2).Value = "Bid Count";
        usersSheet.Cell(buyersStartRow + 1, 3).Value = "Total Bid Amount";

        var buyerRow = buyersStartRow + 2;
        foreach (var buyer in dashboard.UserSection.TopBuyers)
        {
            usersSheet.Cell(buyerRow, 1).Value = buyer.FullName;
            usersSheet.Cell(buyerRow, 2).Value = buyer.BidCount;
            usersSheet.Cell(buyerRow, 3).Value = buyer.TotalBidAmount;
            buyerRow++;
        }

        var sellersHeaderRow = buyerRow + 1;
        usersSheet.Cell(sellersHeaderRow, 1).Value = "Top Sellers";
        usersSheet.Cell(sellersHeaderRow + 1, 1).Value = "Name";
        usersSheet.Cell(sellersHeaderRow + 1, 2).Value = "Listing Count";
        usersSheet.Cell(sellersHeaderRow + 1, 3).Value = "Total Sales";

        var sellerRow = sellersHeaderRow + 2;
        foreach (var seller in dashboard.UserSection.TopSellers)
        {
            usersSheet.Cell(sellerRow, 1).Value = seller.FullName;
            usersSheet.Cell(sellerRow, 2).Value = seller.ListingCount;
            usersSheet.Cell(sellerRow, 3).Value = seller.TotalSales;
            sellerRow++;
        }

        var newUsersHeaderRow = sellerRow + 1;
        usersSheet.Cell(newUsersHeaderRow, 1).Value = "New Registrations";
        usersSheet.Cell(newUsersHeaderRow + 1, 1).Value = "Name";
        usersSheet.Cell(newUsersHeaderRow + 1, 2).Value = "Email";
        usersSheet.Cell(newUsersHeaderRow + 1, 3).Value = "Registered At";
        usersSheet.Cell(newUsersHeaderRow + 1, 4).Value = "Status";

        var newUserRow = newUsersHeaderRow + 2;
        foreach (var user in dashboard.UserSection.NewUsers)
        {
            usersSheet.Cell(newUserRow, 1).Value = user.FullName;
            usersSheet.Cell(newUserRow, 2).Value = user.Email;
            usersSheet.Cell(newUserRow, 3).Value = user.CreatedAt;
            usersSheet.Cell(newUserRow, 4).Value = user.Status;
            newUserRow++;
        }

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
        foreach (var category in dashboard.AuctionSection.CategoryBreakdown)
        {
            auctionsSheet.Cell(categoryRow, 1).Value = category.CategoryName;
            auctionsSheet.Cell(categoryRow, 2).Value = category.BidCount;
            auctionsSheet.Cell(categoryRow, 3).Value = category.BidVolume;
            auctionsSheet.Cell(categoryRow, 4).Value = category.Percentage;
            categoryRow++;
        }

        var recentHeaderRow = categoryRow + 1;
        auctionsSheet.Cell(recentHeaderRow, 1).Value = "Recent Auctions";
        auctionsSheet.Cell(recentHeaderRow + 1, 1).Value = "Product";
        auctionsSheet.Cell(recentHeaderRow + 1, 2).Value = "Seller";
        auctionsSheet.Cell(recentHeaderRow + 1, 3).Value = "Category";
        auctionsSheet.Cell(recentHeaderRow + 1, 4).Value = "Current Bid";
        auctionsSheet.Cell(recentHeaderRow + 1, 5).Value = "Status";
        auctionsSheet.Cell(recentHeaderRow + 1, 6).Value = "Ends In";

        var recentRow = recentHeaderRow + 2;
        foreach (var auction in dashboard.RecentAuctions)
        {
            auctionsSheet.Cell(recentRow, 1).Value = auction.ProductName;
            auctionsSheet.Cell(recentRow, 2).Value = auction.SellerName;
            auctionsSheet.Cell(recentRow, 3).Value = auction.CategoryName;
            auctionsSheet.Cell(recentRow, 4).Value = auction.CurrentPrice;
            auctionsSheet.Cell(recentRow, 5).Value = auction.StatusLabel;
            auctionsSheet.Cell(recentRow, 6).Value = auction.EndsIn;
            recentRow++;
        }

        var revenueDetailSheet = workbook.Worksheets.Add("Revenue Detail");
        revenueDetailSheet.Cell(1, 1).Value = "Date";
        revenueDetailSheet.Cell(1, 2).Value = "Type";
        revenueDetailSheet.Cell(1, 3).Value = "Reference";
        revenueDetailSheet.Cell(1, 4).Value = "GMV";
        revenueDetailSheet.Cell(1, 5).Value = "Platform Revenue";
        revenueDetailSheet.Cell(1, 6).Value = "Status";

        var revenueRow = 2;
        foreach (var row in dashboard.RevenueSection.DetailRows)
        {
            revenueDetailSheet.Cell(revenueRow, 1).Value = row.TransactionDate;
            revenueDetailSheet.Cell(revenueRow, 2).Value = row.Type;
            revenueDetailSheet.Cell(revenueRow, 3).Value = row.ReferenceCode;
            revenueDetailSheet.Cell(revenueRow, 4).Value = row.GmvAmount;
            revenueDetailSheet.Cell(revenueRow, 5).Value = row.PlatformRevenueAmount;
            revenueDetailSheet.Cell(revenueRow, 6).Value = row.Status;
            revenueRow++;
        }

        overviewSheet.Columns().AdjustToContents();
        usersSheet.Columns().AdjustToContents();
        auctionsSheet.Columns().AdjustToContents();
        revenueDetailSheet.Columns().AdjustToContents();

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

    private async Task<DashboardRevenueSectionViewModel> BuildRevenueSectionAsync(
        DashboardFilterViewModel filter,
        DashboardFilterViewModel previousRange,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.DateFrom.Date;
        var rangeEndExclusive = filter.DateTo.Date.AddDays(1);

        var gmvCurrent = await SumGmvAsync(filter, cancellationToken);
        var gmvPrevious = await SumGmvAsync(previousRange, cancellationToken);

        var platformRevenueCurrent = await SumPlatformRevenueAsync(filter, cancellationToken);
        var platformRevenuePrevious = await SumPlatformRevenueAsync(previousRange, cancellationToken);

        var completedOrdersCurrent = await CountCompletedOrdersAsync(rangeStart, rangeEndExclusive, cancellationToken);
        var completedOrdersPrevious = await CountCompletedOrdersAsync(
            previousRange.DateFrom,
            previousRange.DateTo.AddDays(1),
            cancellationToken);

        var registrationDeposits = await SumRegistrationDepositRevenueAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);
        var buyerCheckoutFees = await SumPaidOrderPlatformFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);
        var sellerSuccessFees = await SumPaidOrderSellerFeesAsync(
            rangeStart,
            rangeEndExclusive,
            cancellationToken);

        var lineChart = await BuildRevenueLineChartAsync(rangeStart, rangeEndExclusive, cancellationToken);
        var detailRows = await BuildRevenueDetailTableAsync(filter, cancellationToken);

        return new DashboardRevenueSectionViewModel
        {
            GmvKpi = BuildKpiCard(
                "GMV",
                FormatCurrency(gmvCurrent),
                gmvCurrent,
                gmvPrevious,
                cardKey: DashboardRevenueCardKeys.Gmv),
            PlatformRevenueKpi = BuildKpiCard(
                "Platform Revenue",
                FormatCurrency(platformRevenueCurrent),
                platformRevenueCurrent,
                platformRevenuePrevious,
                cardKey: DashboardRevenueCardKeys.PlatformRevenue),
            CompletedOrdersKpi = BuildKpiCard(
                "Completed Orders",
                FormatInteger(completedOrdersCurrent),
                completedOrdersCurrent,
                completedOrdersPrevious,
                cardKey: DashboardRevenueCardKeys.CompletedOrders),
            LineChart = lineChart,
            PlatformBreakdown = BuildPlatformRevenueBreakdown(
                registrationDeposits,
                buyerCheckoutFees,
                sellerSuccessFees),
            DetailRows = detailRows
        };
    }

    private static DashboardPlatformRevenueBreakdownViewModel BuildPlatformRevenueBreakdown(
        decimal registrationDeposits,
        decimal buyerCheckoutFees,
        decimal sellerSuccessFees)
    {
        var total = registrationDeposits + buyerCheckoutFees + sellerSuccessFees;

        if (total <= 0)
        {
            return new DashboardPlatformRevenueBreakdownViewModel
            {
                RegistrationDeposits = registrationDeposits,
                BuyerCheckoutFees = buyerCheckoutFees,
                SellerSuccessFees = sellerSuccessFees
            };
        }

        return new DashboardPlatformRevenueBreakdownViewModel
        {
            RegistrationDeposits = registrationDeposits,
            RegistrationDepositsPercentage = Math.Round(registrationDeposits / total * 100m, 1),
            BuyerCheckoutFees = buyerCheckoutFees,
            BuyerCheckoutFeesPercentage = Math.Round(buyerCheckoutFees / total * 100m, 1),
            SellerSuccessFees = sellerSuccessFees,
            SellerSuccessFeesPercentage = Math.Round(sellerSuccessFees / total * 100m, 1)
        };
    }

    private async Task<List<DashboardRevenueLinePointViewModel>> BuildRevenueLineChartAsync(
        DateTime startDate,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var gmvByDate = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startDate
                              && payment.PaidAt < endExclusive)
            .GroupBy(payment => payment.PaidAt!.Value.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(payment => payment.Amount) })
            .ToListAsync(cancellationToken);

        var buyerFeesByDate = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startDate
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .GroupBy(payment => payment.PaidAt!.Value.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(payment => payment.Order.PlatformFee) })
            .ToListAsync(cancellationToken);

        var sellerFeesByDate = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startDate
                              && payment.PaidAt < endExclusive
                              && PaidOrderStatuses.Contains(payment.Order.Status))
            .GroupBy(payment => payment.PaidAt!.Value.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(payment => payment.Order.SellerFee) })
            .ToListAsync(cancellationToken);

        var depositsByDate = await _dbContext.AuctionRegistrationDeposits.AsNoTracking()
            .Where(deposit => deposit.DeletedAt == null
                              && deposit.PaidAt >= startDate
                              && deposit.PaidAt < endExclusive
                              && (deposit.Status == AuctionRegistrationDepositStatuses.Paid
                                  || deposit.Status == AuctionRegistrationDepositStatuses.Applied))
            .GroupBy(deposit => deposit.PaidAt!.Value.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(deposit => deposit.Amount) })
            .ToListAsync(cancellationToken);

        var gmvLookup = gmvByDate.ToDictionary(item => item.Date, item => item.Total);
        var buyerFeeLookup = buyerFeesByDate.ToDictionary(item => item.Date, item => item.Total);
        var sellerFeeLookup = sellerFeesByDate.ToDictionary(item => item.Date, item => item.Total);
        var depositLookup = depositsByDate.ToDictionary(item => item.Date, item => item.Total);

        var series = new List<DashboardRevenueLinePointViewModel>();
        var endDate = endExclusive.AddDays(-1).Date;

        for (var cursor = startDate.Date; cursor <= endDate; cursor = cursor.AddDays(1))
        {
            gmvLookup.TryGetValue(cursor, out var gmv);
            buyerFeeLookup.TryGetValue(cursor, out var buyerFees);
            sellerFeeLookup.TryGetValue(cursor, out var sellerFees);
            depositLookup.TryGetValue(cursor, out var deposits);

            series.Add(new DashboardRevenueLinePointViewModel
            {
                Label = cursor.ToString("MMM d", CultureInfo.InvariantCulture),
                FilterKey = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Gmv = gmv,
                PlatformRevenue = buyerFees + sellerFees + deposits
            });
        }

        return series;
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

    private async Task<decimal> SumRegistrationDepositRevenueAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuctionRegistrationDeposits.AsNoTracking()
            .Where(deposit => deposit.DeletedAt == null
                              && deposit.PaidAt >= startInclusive
                              && deposit.PaidAt < endExclusive
                              && (deposit.Status == AuctionRegistrationDepositStatuses.Paid
                                  || deposit.Status == AuctionRegistrationDepositStatuses.Applied))
            .SumAsync(deposit => deposit.Amount, cancellationToken);
    }

    private Task<int> CountCompletedOrdersAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        return _dbContext.Orders.AsNoTracking()
            .CountAsync(
                order => order.DeletedAt == null
                         && PaidOrderStatuses.Contains(order.Status)
                         && order.CreatedAt >= startInclusive
                         && order.CreatedAt < endExclusive,
                cancellationToken);
    }

    private static string? NormalizeRevenueTypeFilter(string? revenueTypeFilter)
    {
        if (string.IsNullOrWhiteSpace(revenueTypeFilter))
        {
            return null;
        }

        var normalized = revenueTypeFilter.Trim().ToLowerInvariant();

        return normalized switch
        {
            DashboardRevenueTypes.OrderPayment => DashboardRevenueTypes.OrderPayment,
            DashboardRevenueTypes.RegistrationDeposit => DashboardRevenueTypes.RegistrationDeposit,
            DashboardRevenueTypes.SellerSuccessFee => DashboardRevenueTypes.SellerSuccessFee,
            _ => null
        };
    }

    private static List<DashboardRevenueDetailViewModel> ApplyRevenueTypeFilter(
        List<DashboardRevenueDetailViewModel> rows,
        string? revenueTypeFilter)
    {
        if (string.IsNullOrWhiteSpace(revenueTypeFilter))
        {
            return rows;
        }

        return rows
            .Where(row => row.Type == revenueTypeFilter)
            .ToList();
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

    private async Task<List<DashboardNewUserViewModel>> GetNewUsersAsync(
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
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => new DashboardNewUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt,
                Status = user.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    private static List<DashboardNewUserViewModel> ApplyRegistrationChartFilter(
        IReadOnlyList<DashboardNewUserViewModel> users,
        DashboardFilterViewModel filter)
    {
        if (!filter.RegistrationDateFilter.HasValue)
        {
            return users.ToList();
        }

        var target = filter.RegistrationDateFilter.Value.Date;
        var granularity = filter.RegistrationGranularity;

        return users
            .Where(user => MatchesRegistrationFilter(user.CreatedAt, target, granularity))
            .ToList();
    }

    private static bool MatchesRegistrationFilter(DateTime createdAt, DateTime target, string granularity)
    {
        var date = createdAt.Date;

        return granularity switch
        {
            "week" => ISOWeek.GetYear(date) == ISOWeek.GetYear(target)
                      && ISOWeek.GetWeekOfYear(date) == ISOWeek.GetWeekOfYear(target),
            "month" => date.Year == target.Year && date.Month == target.Month,
            _ => date == target
        };
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

    private async Task<int> CountActiveAuctionsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                             && auction.Product.DeletedAt == null
                             && ActiveAuctionStatuses.Contains(auction.Status),
                cancellationToken);
    }

    private async Task<int> CountActiveAuctionsAtAsync(DateTime pointInTime, CancellationToken cancellationToken)
    {
        return await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                             && auction.Product.DeletedAt == null
                             && auction.StartDate <= pointInTime
                             && auction.EndDate > pointInTime
                             && auction.Status != AuctionStatuses.Cancelled,
                cancellationToken);
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

    private async Task<List<DashboardChartPointViewModel>> BuildDailyBidSeriesAsync(
        DateTime startDate,
        DateTime endExclusive,
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.PlacedAt >= startDate
                          && bid.PlacedAt < endExclusive)
            .GroupBy(bid => bid.PlacedAt.Date)
            .Select(group => new { Date = group.Key, Total = group.Count() })
            .ToListAsync(cancellationToken);

        return BuildDailySeries(
            startDate,
            endExclusive,
            grouped.ToDictionary(item => item.Date, item => (decimal)item.Total));
    }

    private async Task<List<DashboardStatusBreakdownViewModel>> BuildStatusBreakdownAsync(
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null)
            .GroupBy(auction => auction.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return grouped
            .OrderByDescending(item => item.Count)
            .Select(item => new DashboardStatusBreakdownViewModel
            {
                Status = item.Status,
                Label = FormatStatusLabel(item.Status),
                Count = item.Count
            })
            .ToList();
    }

    private static List<DashboardChartPointViewModel> BuildDailySeries(
        DateTime startDate,
        DateTime endExclusive,
        Dictionary<DateTime, decimal> valuesByDate)
    {
        var series = new List<DashboardChartPointViewModel>();
        var endDate = endExclusive.AddDays(-1).Date;

        for (var cursor = startDate.Date; cursor <= endDate; cursor = cursor.AddDays(1))
        {
            valuesByDate.TryGetValue(cursor, out var value);

            series.Add(new DashboardChartPointViewModel
            {
                Label = cursor.ToString("MMM d", CultureInfo.InvariantCulture),
                Value = value
            });
        }

        return series;
    }

    private static DashboardKpiCardViewModel BuildSnapshotKpi(
        string label,
        string displayValue,
        string? linkUrl = null)
    {
        return new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
            LinkUrl = linkUrl,
            ChangeDisplay = string.Empty
        };
    }

    private static DashboardKpiCardViewModel BuildKpiCard(
        string label,
        string displayValue,
        decimal currentValue,
        decimal previousValue,
        bool includeChange = true,
        string? linkUrl = null,
        string? cardKey = null)
    {
        var card = new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
            LinkUrl = linkUrl,
            CardKey = cardKey
        };

        if (!includeChange)
        {
            card.ChangeDisplay = string.Empty;
            return card;
        }

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

    private static string GetStatusBadgeClass(string status) => status switch
    {
        AuctionStatuses.PendingReview => "bg-warning-50 text-warning-600 dark:bg-warning-500/15 dark:text-warning-400",
        AuctionStatuses.Rejected => "bg-error-50 text-error-600 dark:bg-error-500/15 dark:text-error-500",
        AuctionStatuses.Scheduled => "bg-brand-50 text-brand-700 dark:bg-brand-500/15 dark:text-brand-400",
        AuctionStatuses.Live => "bg-success-50 text-success-600 dark:bg-success-500/15 dark:text-success-500",
        AuctionStatuses.EndingSoon => "bg-warning-50 text-warning-600 dark:bg-warning-500/15 dark:text-warning-400",
        AuctionStatuses.AwaitingPayment => "bg-warning-50 text-warning-600 dark:bg-warning-500/15 dark:text-warning-400",
        AuctionStatuses.Completed => "bg-brand-50 text-brand-700 dark:bg-brand-500/15 dark:text-brand-400",
        _ => "bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-gray-400"
    };

    private static string FormatEndsIn(DateTime endDateUtc, string status, DateTime utcNow)
    {
        if (status is AuctionStatuses.Ended
            or AuctionStatuses.Completed
            or AuctionStatuses.Cancelled
            or AuctionStatuses.AwaitingPayment)
        {
            return "Ended";
        }

        if (utcNow >= endDateUtc)
        {
            return "Ended";
        }

        var remaining = endDateUtc - utcNow;

        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }

        return $"{Math.Max(remaining.Minutes, 1)}m";
    }
}
