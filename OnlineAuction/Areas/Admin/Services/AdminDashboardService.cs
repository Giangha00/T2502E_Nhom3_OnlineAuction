using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Dashboard;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private const int ChartDays = 7;
    private const int RecentAuctionCount = 10;
    private const int ComparisonDays = 7;

    private static readonly string[] ActiveAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private readonly AuctionHouseDbContext _dbContext;

    public AdminDashboardService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var todayStart = utcNow.Date;
        var yesterdayStart = todayStart.AddDays(-1);
        var comparisonStart = todayStart.AddDays(-ComparisonDays);
        var previousComparisonStart = todayStart.AddDays(-ComparisonDays * 2);
        var chartStart = todayStart.AddDays(-(ChartDays - 1));
        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var comparisonPoint = todayStart.AddDays(-ComparisonDays);

        var activeAuctionsNow = await CountActiveAuctionsAsync(utcNow, cancellationToken);
        var activeAuctionsPrevious = await CountActiveAuctionsAtAsync(comparisonPoint, cancellationToken);

        var usersCurrentPeriod = await _dbContext.Users.AsNoTracking()
            .CountAsync(
                user => user.DeletedAt == null
                        && user.Status == UserStatus.Active
                        && user.CreatedAt >= comparisonStart,
                cancellationToken);

        var usersPreviousPeriod = await _dbContext.Users.AsNoTracking()
            .CountAsync(
                user => user.DeletedAt == null
                        && user.Status == UserStatus.Active
                        && user.CreatedAt >= previousComparisonStart
                        && user.CreatedAt < comparisonStart,
                cancellationToken);

        var totalActiveUsers = await _dbContext.Users.AsNoTracking()
            .CountAsync(
                user => user.DeletedAt == null && user.Status == UserStatus.Active,
                cancellationToken);

        var bidsToday = await _dbContext.Bids.AsNoTracking()
            .CountAsync(
                bid => bid.DeletedAt == null && bid.PlacedAt >= todayStart,
                cancellationToken);

        var bidsYesterday = await _dbContext.Bids.AsNoTracking()
            .CountAsync(
                bid => bid.DeletedAt == null
                       && bid.PlacedAt >= yesterdayStart
                       && bid.PlacedAt < todayStart,
                cancellationToken);

        var revenueCurrentPeriod = await SumSuccessfulPaymentsAsync(comparisonStart, utcNow, cancellationToken);
        var revenuePreviousPeriod = await SumSuccessfulPaymentsAsync(previousComparisonStart, comparisonStart, cancellationToken);

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

        var completedOrdersThisMonth = await _dbContext.Orders.AsNoTracking()
            .CountAsync(
                order => order.DeletedAt == null
                         && (order.Status == OrderStatuses.Paid || order.Status == OrderStatuses.Delivered)
                         && order.CreatedAt >= monthStart,
                cancellationToken);

        var kpiCards = new List<DashboardKpiCardViewModel>
        {
            BuildKpiCard("Active Auctions", FormatInteger(activeAuctionsNow), activeAuctionsNow, activeAuctionsPrevious),
            BuildKpiCard("Registered Users", FormatInteger(totalActiveUsers), usersCurrentPeriod, usersPreviousPeriod),
            BuildKpiCard("Total Bids Today", FormatInteger(bidsToday), bidsToday, bidsYesterday),
            BuildKpiCard("Revenue (USD)", FormatCurrency(revenueCurrentPeriod), revenueCurrentPeriod, revenuePreviousPeriod)
        };

        var secondaryKpiCards = new List<DashboardKpiCardViewModel>
        {
            BuildKpiCard("Pending Verifications", FormatInteger(pendingVerifications), pendingVerifications, 0, includeChange: false, linkUrl: "/Admin/AuctionVerification"),
            BuildKpiCard("Pending Payments", FormatInteger(pendingPayments), pendingPayments, 0, includeChange: false),
            BuildKpiCard("Pending Registrations", FormatInteger(pendingRegistrations), pendingRegistrations, 0, includeChange: false),
            BuildKpiCard("Completed Orders (Month)", FormatInteger(completedOrdersThisMonth), completedOrdersThisMonth, 0, includeChange: false)
        };

        var recentAuctions = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null)
            .OrderByDescending(auction => auction.CreatedAt)
            .Take(RecentAuctionCount)
            .Select(auction => new
            {
                auction.Id,
                auction.Product.Name,
                SellerName = auction.Product.Seller.FullName,
                auction.CurrentPrice,
                auction.Status,
                auction.EndDate
            })
            .ToListAsync(cancellationToken);

        var revenueChart = await BuildDailyPaymentSeriesAsync(chartStart, utcNow, cancellationToken);
        var bidsChart = await BuildDailyBidSeriesAsync(chartStart, utcNow, cancellationToken);
        var statusBreakdown = await BuildStatusBreakdownAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            KpiCards = kpiCards,
            SecondaryKpiCards = secondaryKpiCards,
            RecentAuctions = recentAuctions
                .Select(auction => new DashboardRecentAuctionViewModel
                {
                    Id = auction.Id,
                    ProductName = auction.Name,
                    SellerName = auction.SellerName,
                    CurrentPrice = auction.CurrentPrice,
                    Status = auction.Status,
                    StatusLabel = FormatStatusLabel(auction.Status),
                    StatusBadgeClass = GetStatusBadgeClass(auction.Status),
                    EndsIn = FormatEndsIn(auction.EndDate, auction.Status, utcNow)
                })
                .ToList(),
            RevenueChart = revenueChart,
            BidsChart = bidsChart,
            AuctionStatusBreakdown = statusBreakdown
        };
    }

    public async Task<byte[]> ExportSummaryCsvAsync(int periodDays = 30, CancellationToken cancellationToken = default)
    {
        if (periodDays <= 0)
        {
            periodDays = 30;
        }

        var utcNow = DateTime.UtcNow;
        var periodStart = utcNow.Date.AddDays(-(periodDays - 1));
        var dashboard = await GetDashboardAsync(cancellationToken);

        var revenueTotal = await SumSuccessfulPaymentsAsync(periodStart, utcNow, cancellationToken);
        var bidCount = await _dbContext.Bids.AsNoTracking()
            .CountAsync(
                bid => bid.DeletedAt == null && bid.PlacedAt >= periodStart,
                cancellationToken);

        var auctionsInPeriod = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null
                              && auction.Product.DeletedAt == null
                              && auction.CreatedAt >= periodStart)
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => new
            {
                auction.Id,
                auction.Product.Name,
                SellerName = auction.Product.Seller.FullName,
                auction.CurrentPrice,
                auction.Status,
                auction.EndDate
            })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Section,Metric,Value");
        builder.AppendLine($"Summary,Period Days,{periodDays}");
        builder.AppendLine($"Summary,Generated At (UTC),{utcNow:O}");

        foreach (var card in dashboard.KpiCards.Concat(dashboard.SecondaryKpiCards))
        {
            builder.AppendLine($"KPI,{EscapeCsv(card.Label)},{EscapeCsv(card.DisplayValue)}");
        }

        builder.AppendLine($"Summary,Revenue ({periodDays}d),{revenueTotal:F2}");
        builder.AppendLine($"Summary,Bid Count ({periodDays}d),{bidCount}");
        builder.AppendLine();
        builder.AppendLine("Auction ID,Product,Seller,Current Price,Status,End Date (UTC)");

        foreach (var auction in auctionsInPeriod)
        {
            builder.AppendLine(string.Join(',',
                auction.Id,
                EscapeCsv(auction.Name),
                EscapeCsv(auction.SellerName),
                auction.CurrentPrice.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsv(auction.Status),
                auction.EndDate.ToString("O", CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private async Task<int> CountActiveAuctionsAsync(DateTime utcNow, CancellationToken cancellationToken)
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

    private async Task<List<DashboardChartPointViewModel>> BuildDailyPaymentSeriesAsync(
        DateTime startDate,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Payments.AsNoTracking()
            .Where(payment => payment.DeletedAt == null
                              && payment.Status == PaymentStatuses.Success
                              && payment.PaidAt >= startDate
                              && payment.PaidAt <= utcNow)
            .GroupBy(payment => payment.PaidAt!.Value.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(payment => payment.Amount) })
            .ToListAsync(cancellationToken);

        return BuildDailySeries(startDate, ChartDays, grouped.ToDictionary(item => item.Date, item => item.Total));
    }

    private async Task<List<DashboardChartPointViewModel>> BuildDailyBidSeriesAsync(
        DateTime startDate,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Bids.AsNoTracking()
            .Where(bid => bid.DeletedAt == null
                          && bid.PlacedAt >= startDate
                          && bid.PlacedAt <= utcNow)
            .GroupBy(bid => bid.PlacedAt.Date)
            .Select(group => new { Date = group.Key, Total = group.Count() })
            .ToListAsync(cancellationToken);

        return BuildDailySeries(
            startDate,
            ChartDays,
            grouped.ToDictionary(item => item.Date, item => (decimal)item.Total));
    }

    private async Task<List<DashboardStatusBreakdownViewModel>> BuildStatusBreakdownAsync(
        CancellationToken cancellationToken)
    {
        var grouped = await _dbContext.Auctions.AsNoTracking()
            .Where(auction => auction.DeletedAt == null)
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
        int dayCount,
        Dictionary<DateTime, decimal> valuesByDate)
    {
        var series = new List<DashboardChartPointViewModel>(dayCount);

        for (var offset = 0; offset < dayCount; offset++)
        {
            var date = startDate.AddDays(offset).Date;
            valuesByDate.TryGetValue(date, out var value);

            series.Add(new DashboardChartPointViewModel
            {
                Label = date.ToString("MMM d", CultureInfo.InvariantCulture),
                Value = value
            });
        }

        return series;
    }

    private static DashboardKpiCardViewModel BuildKpiCard(
        string label,
        string displayValue,
        decimal currentValue,
        decimal previousValue,
        bool includeChange = true,
        string? linkUrl = null)
    {
        var card = new DashboardKpiCardViewModel
        {
            Label = label,
            DisplayValue = displayValue,
            LinkUrl = linkUrl
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

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
