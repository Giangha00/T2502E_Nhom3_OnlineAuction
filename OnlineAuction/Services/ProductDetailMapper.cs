using OnlineAuction.Entities;
using OnlineAuction.Models;

namespace OnlineAuction.Services;

internal static class ProductDetailMapper
{
    public static ProductDetailViewModel MapToViewModel(
        Auction auction,
        SellerViewModel seller,
        IReadOnlyList<AuctionItemViewModel> relatedProducts)
    {
        var product = auction.Product;
        var bids = auction.Bids.OrderByDescending(b => b.PlacedAt).ToList();
        var (days, hours, minutes, seconds) = CalculateCountdown(auction.EndDate);
        var (auctionStatus, badgeClass) = MapAuctionStatus(auction.Status, auction.EndDate);

        return new ProductDetailViewModel
        {
            Id = auction.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription ?? BuildDefaultShortDescription(product),
            Category = GetCategoryName(product),
            Condition = FormatCondition(product.Condition),
            Grade = product.GradeLabel ?? "—",
            Subtitle = BuildSubtitle(product),
            Year = product.Year ?? 0,
            SetName = product.SetName ?? "—",
            Language = "—",
            CardNumber = "—",
            CertificateNumber = product.CertNumber ?? "—",
            DescriptionHtml = BuildDescriptionHtml(product, auction),
            Images = string.IsNullOrWhiteSpace(product.PrimaryImage)
                ? []
                : [product.PrimaryImage],
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.CurrentPrice,
            BidStep = auction.BidStep,
            BidCount = bids.Count,
            LotNumber = 0,
            WatcherCount = 0,
            EstimatedValue = 0,
            ReserveMet = auction.CurrentPrice >= auction.StartingPrice,
            AuctionEventName = "RareCard Vault: Premium Trading Card Auction",
            QuickBidAmounts = BuildQuickBidAmounts(auction.CurrentPrice, auction.BidStep),
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            CountdownDays = days,
            CountdownHours = hours,
            CountdownMinutes = minutes,
            CountdownSeconds = seconds,
            AuctionStatus = auctionStatus,
            StatusBadgeClass = badgeClass,
            CanPlaceBid = CanAcceptBids(auction),
            Seller = seller,
            Grading = BuildGrading(product.GradeLabel),
            BidHistory = MapBidHistory(bids),
            Documents = [],
            RelatedProducts = relatedProducts.ToList()
        };
    }

    public static SellerViewModel MapSeller(ApplicationUser seller, int auctionCount, int successfulSales) =>
        new()
        {
            Id = seller.Id,
            Username = seller.UserName ?? seller.Email ?? "Seller",
            AvatarUrl = seller.AvatarUrl ?? "/admin/images/user/user-01.jpg",
            AuctionCount = auctionCount,
            SuccessfulSales = successfulSales,
            Rating = 0
        };

    public static AuctionItemViewModel MapToAuctionItem(Auction auction)
    {
        var product = auction.Product;
        return new AuctionItemViewModel
        {
            Id = auction.Id,
            Name = product.Name,
            Category = GetCategoryName(product),
            ImageUrl = product.PrimaryImage,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.CurrentPrice,
            Status = MapCardStatus(auction.Status),
            TimeRemaining = FormatTimeRemaining(auction.EndDate),
            Grade = product.GradeLabel ?? string.Empty,
            Subtitle = BuildSubtitle(product),
            Condition = FormatCondition(product.Condition),
            Year = product.Year ?? 0,
            IsHot = auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon
        };
    }

    public static List<BidHistoryItemViewModel> MapBidHistory(IEnumerable<Bid> bids) =>
        bids.Select(bid => new BidHistoryItemViewModel
        {
            BidderName = FormatBidderName(bid.Bidder),
            Amount = bid.Amount,
            BidTime = bid.PlacedAt,
            Status = bid.IsWinning ? "WINNING" : "OUTBID"
        }).ToList();

    private static string FormatBidderName(ApplicationUser bidder)
    {
        var display = string.IsNullOrWhiteSpace(bidder.FullName)
            ? bidder.UserName ?? "Bidder"
            : bidder.FullName;

        if (display.Length <= 2)
        {
            return display;
        }

        return $"{display[0]}***{display[^1]}";
    }

    private static GradingScoreViewModel BuildGrading(string? gradeLabel)
    {
        var numeric = gradeLabel switch
        {
            null or "" => "—",
            var g when g.Contains("10", StringComparison.Ordinal) => "10",
            var g when g.Contains("9.5", StringComparison.Ordinal) => "9.5",
            var g when g.Contains('9') => "9",
            var g when g.Contains("8.5", StringComparison.Ordinal) => "8.5",
            var g when g.Contains('8') => "8",
            _ => "—"
        };

        return new GradingScoreViewModel
        {
            Centering = numeric,
            Corners = numeric,
            Edges = numeric,
            Surface = numeric
        };
    }

    private static List<decimal> BuildQuickBidAmounts(decimal currentPrice, decimal bidStep) =>
    [
        currentPrice + bidStep,
        currentPrice + bidStep * 2,
        currentPrice + bidStep * 5
    ];

    private static (int Days, int Hours, int Minutes, int Seconds) CalculateCountdown(DateTime endDate)
    {
        var remaining = endDate.ToUniversalTime() - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return (0, 0, 0, 0);
        }

        return (
            remaining.Days,
            remaining.Hours,
            remaining.Minutes,
            remaining.Seconds);
    }

    public static bool CanAcceptBids(Auction auction) =>
        auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon &&
        auction.EndDate.ToUniversalTime() > DateTime.UtcNow;

    private static (string Status, string BadgeClass) MapAuctionStatus(string status, DateTime endDate)
    {
        if (endDate.ToUniversalTime() <= DateTime.UtcNow &&
            status is not AuctionStatuses.Cancelled)
        {
            return ("Ended", "bg-stone-600 text-white");
        }

        return status switch
        {
            AuctionStatuses.Live => ("Active Auction", "bg-emerald-600 text-white"),
            AuctionStatuses.EndingSoon => ("Ending Soon", "bg-orange-600 text-white"),
            AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment => ("Ended", "bg-stone-600 text-white"),
            AuctionStatuses.Completed => ("Completed", "bg-stone-600 text-white"),
            AuctionStatuses.Cancelled => ("Cancelled", "bg-stone-500 text-white"),
            _ => ("Active Auction", "bg-emerald-600 text-white")
        };
    }

    private static string MapCardStatus(string status) => status switch
    {
        AuctionStatuses.EndingSoon => "Ending Soon",
        AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment => "Ended",
        AuctionStatuses.Completed => "Completed",
        AuctionStatuses.Cancelled => "Cancelled",
        _ => "Live"
    };

    private static string FormatTimeRemaining(DateTime endDate)
    {
        var remaining = endDate.ToUniversalTime() - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Ended";
        }

        if (remaining.TotalDays >= 1)
        {
            return $"{remaining.Days}d {remaining.Hours}h left";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{remaining.Hours}h {remaining.Minutes}m left";
        }

        return $"{Math.Max(remaining.Minutes, 1)}m left";
    }

    private static string BuildSubtitle(Product product)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.SetName))
        {
            parts.Add(product.SetName);
        }

        if (!string.IsNullOrWhiteSpace(product.GradeLabel))
        {
            parts.Add(product.GradeLabel);
        }

        if (product.Year.HasValue && product.Year.Value > 0)
        {
            parts.Add(product.Year.Value.ToString());
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : GetCategoryName(product);
    }

    private static string FormatCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return "Graded";
        }

        return char.ToUpperInvariant(condition[0]) + (condition.Length > 1 ? condition[1..] : string.Empty);
    }

    private static string GetCategoryName(Product product) =>
        product.Category?.Name ?? string.Empty;

    private static string BuildDefaultShortDescription(Product product) =>
        $"Authenticated {GetCategoryName(product)} listing from RareCard Vault.";

    private static string BuildDescriptionHtml(Product product, Auction auction)
    {
        if (!string.IsNullOrWhiteSpace(product.DescriptionHtml))
        {
            return product.DescriptionHtml;
        }

        return $"""
            <p>{BuildDefaultShortDescription(product)}</p>
            <h3>Listing details</h3>
            <ul>
                <li>Category: {GetCategoryName(product)}</li>
                <li>Grade: {product.GradeLabel ?? "—"}</li>
                <li>Starting price: ${auction.StartingPrice:N0}</li>
                <li>Current price: ${auction.CurrentPrice:N0}</li>
            </ul>
            """;
    }
}
