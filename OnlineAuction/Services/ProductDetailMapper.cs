using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;

namespace OnlineAuction.Services;

internal static class ProductDetailMapper
{
    private const string DefaultProductImageUrl =
        "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop";

    private const int HotBidCountThreshold = 5;
    public static ProductDetailViewModel MapToViewModel(
        Auction auction,
        SellerViewModel seller,
        IReadOnlyList<AuctionItemViewModel> relatedProducts,
        IReadOnlyList<AuctionItemViewModel>? moreRelatedProducts = null,
        int? currentUserId = null,
        string? userRegistrationStatus = null,
        string? registrationRejectReason = null,
        int registrationCount = 0,
        PlatformFeeSettings? feeSettings = null)
    {
        var product = auction.Product;
        var bids = auction.Bids.OrderByDescending(b => b.PlacedAt).ToList();
        var phaseInfo = AuctionScheduleHelper.ResolveListingPhase(auction);
        var countdownTarget = phaseInfo.CountdownTarget;
        var (days, hours, minutes, seconds) = CalculateCountdown(countdownTarget);
        var (auctionStatus, badgeClass) = MapAuctionStatus(auction.Status, auction.EndDate);
        var isSeller = currentUserId.HasValue && product.SellerId == currentUserId.Value;
        var auctionAcceptsBids = CanAcceptBids(auction);
        var canRegister = AuctionScheduleHelper.IsRegistrationOpen(auction);
        var canBid = ComputeCanBid(
            auction,
            currentUserId,
            userRegistrationStatus,
            isSeller,
            auctionAcceptsBids);
        var isRegistered = userRegistrationStatus == AuctionRegistrationStatuses.Approved;
        var registrationDepositAmount = ResolveRegistrationDepositAmount(auction, feeSettings);

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
            Language = product.Language ?? "—",
            CardNumber = product.CardNumber ?? "—",
            CertificateNumber = product.CertNumber ?? "—",
            DescriptionHtml = BuildDescriptionHtml(product, auction),
            Images = BuildImageGallery(product),
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.CurrentPrice,
            BidStep = auction.BidStep,
            BidCount = bids.Count,
            LotNumber = 0,
            WatcherCount = registrationCount,
            ReserveMet = auction.CurrentPrice >= auction.StartingPrice,
            AuctionEventName = string.IsNullOrWhiteSpace(auction.AuctionEventName)
                ? "RareCard Vault: Premium Trading Card Auction"
                : auction.AuctionEventName!,
            QuickBidAmounts = BuildQuickBidAmounts(auction.CurrentPrice, auction.BidStep),
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            RegistrationStartDate = auction.RegistrationStartDate,
            RegistrationEndDate = auction.RegistrationEndDate,
            CountdownTargetDate = countdownTarget,
            ListingPhase = phaseInfo.Phase,
            PhaseCountdownKind = phaseInfo.CountdownKind,
            CountdownDays = days,
            CountdownHours = hours,
            CountdownMinutes = minutes,
            CountdownSeconds = seconds,
            AuctionStatus = auctionStatus,
            StatusBadgeClass = badgeClass,
            CanPlaceBid = auctionAcceptsBids,
            CanRegister = canRegister,
            RequiresRegistration = auction.RequiresRegistration,
            IsRegistered = isRegistered,
            RegistrationStatus = userRegistrationStatus,
            RegistrationRejectReason = registrationRejectReason,
            CanBid = canBid,
            IsSeller = isSeller,
            IsVerifiedAuthentic = auction.VerifiedAt.HasValue,
            RegistrationCount = registrationCount,
            RegistrationDepositAmount = registrationDepositAmount,
            Seller = seller,
            Grading = BuildGrading(product),
            BidHistory = MapBidHistory(
                bids,
                ShouldRevealBidderIdentity(auction),
                BidHistoryPreviewLimit),
            Documents = MapDocuments(product),
            RelatedProducts = relatedProducts.ToList(),
            MoreRelatedProducts = (moreRelatedProducts ?? []).ToList(),
            BuyNowPrice = auction.BuyNowPrice,
            ListingType = auction.ListingType,
            ListingRejectReason = auction.RejectReason
        };
    }

    public static bool ComputeCanBid(
        Auction auction,
        int? currentUserId,
        string? registrationStatus,
        bool isSeller,
        bool auctionAcceptsBids)
    {
        if (!auctionAcceptsBids || !currentUserId.HasValue || isSeller)
        {
            return false;
        }

        if (!auction.RequiresRegistration)
        {
            return true;
        }

        return registrationStatus == AuctionRegistrationStatuses.Approved;
    }

    private static decimal ResolveRegistrationDepositAmount(Auction auction, PlatformFeeSettings? feeSettings)
    {
        if (!auction.RequiresRegistration || feeSettings is null)
        {
            return 0m;
        }

        var productValue = auction.Product.EstimatedValue ?? auction.StartingPrice;
        if (productValue <= 0)
        {
            return 0m;
        }

        return MarketplaceFeeCalculator.CalculateRegistrationDeposit(productValue, feeSettings);
    }

    public static SellerViewModel MapSeller(ApplicationUser seller, int auctionCount, int successfulSales) =>
        new()
        {
            Id = seller.Id,
            Username = seller.UserName ?? seller.Email ?? "Seller",
            FullName = seller.FullName,
            AvatarUrl = seller.AvatarUrl ?? "/admin/images/user/user-01.jpg",
            AuctionCount = auctionCount,
            SuccessfulSales = successfulSales
        };

    public static AuctionItemViewModel MapToAuctionItem(Auction auction, bool forBuyNowCatalog = false)
    {
        var product = auction.Product;
        var bidCount = auction.Bids?.Count ?? 0;
        var phaseInfo = ResolveDisplayPhase(auction);
        var status = AuctionStatuses.IsConfirming(auction.Status)
            ? "Confirming"
            : auction.Status switch
            {
                AuctionStatuses.Rejected => "Rejected",
                AuctionStatuses.Cancelled => "Cancelled",
                _ => MapListingStatus(auction, phaseInfo)
            };
        var hasBuyNow = auction.BuyNowPrice.HasValue && auction.BuyNowPrice.Value > 0;
        var countdownTarget = forBuyNowCatalog || auction.ListingType == ListingTypes.BuyNow
            ? auction.EndDate
            : phaseInfo.CountdownTarget;

        var item = new AuctionItemViewModel
        {
            Id = auction.Id,
            Name = product.Name,
            Category = GetCategoryName(product),
            ImageUrl = ResolveImageUrl(product.PrimaryImage),
            StartingPrice = auction.StartingPrice,
            CurrentPrice = forBuyNowCatalog && hasBuyNow
                ? auction.BuyNowPrice!.Value
                : auction.ListingType == ListingTypes.BuyNow && auction.BuyNowPrice is > 0
                    ? auction.BuyNowPrice.Value
                    : auction.CurrentPrice,
            Status = status,
            ListingPhase = phaseInfo.Phase,
            PhaseCountdownKind = phaseInfo.CountdownKind,
            TimeRemaining = forBuyNowCatalog || auction.ListingType == ListingTypes.BuyNow
                ? "In stock"
                : FormatTimeRemaining(countdownTarget),
            EndDate = countdownTarget,
            ListingType = auction.ListingType,
            BuyNowPrice = auction.BuyNowPrice ?? (auction.ListingType == ListingTypes.BuyNow ? auction.CurrentPrice : null),
            Grade = product.GradeLabel ?? string.Empty,
            Authenticator = ResolveAuthenticator(product.GradeLabel),
            Subtitle = BuildSubtitle(product),
            Condition = FormatCondition(product.Condition),
            Year = product.Year ?? 0,
            BidCount = bidCount,
            IsHot = status == "Ending Soon" || bidCount >= HotBidCountThreshold
        };

        // Expose whether auction requires registration for listing summaries
        item.RequiresRegistration = auction.RequiresRegistration;

        ApplyDealInfo(item);
        return item;
    }

    /// <summary>
    /// Pending listings should render like the sell-form preview (schedule phases),
    /// not as a broken "upcoming" card while waiting for admin approval.
    /// </summary>
    private static AuctionListingPhaseInfo ResolveDisplayPhase(Auction auction)
    {
        if (auction.Status != AuctionStatuses.Confirming &&
            auction.Status != AuctionStatuses.LegacyPendingReview)
        {
            return AuctionScheduleHelper.ResolveListingPhase(auction);
        }

        var originalStatus = auction.Status;
        auction.Status = AuctionStatuses.Scheduled;
        try
        {
            return AuctionScheduleHelper.ResolveListingPhase(auction);
        }
        finally
        {
            auction.Status = originalStatus;
        }
    }

    private static string MapListingStatus(Auction auction, AuctionListingPhaseInfo phaseInfo) =>
        phaseInfo.Phase switch
        {
            AuctionListingPhases.LiveEndingSoon => "Ending Soon",
            AuctionListingPhases.LiveAuction => "Live",
            AuctionListingPhases.RegistrationOpen => "Registration Open",
            AuctionListingPhases.Upcoming => "Upcoming",
            AuctionListingPhases.Ended => "Ended",
            _ => MapCardStatus(auction)
        };

    public static void ApplyDealInfo(AuctionItemViewModel item)
    {
        item.DisplayTitle = BuildListingTitle(item);

        if (!string.IsNullOrWhiteSpace(item.DealLabel))
        {
            return;
        }

        if (item.ListingType == ListingTypes.BuyNow && item.StartingPrice > 0)
        {
            var savings = item.StartingPrice - item.CurrentPrice;
            if (savings >= item.StartingPrice * 0.12m)
            {
                item.DealLabel = "Great Deal";
                item.DealNote = savings > 0
                    ? $"${savings:N0} below list"
                    : "Offers being negotiated";
                return;
            }

            if (savings > 0)
            {
                item.DealLabel = "Good Deal";
                item.DealNote = "Offers being negotiated";
                return;
            }
        }

        if (item.BuyNowPrice.HasValue && item.BuyNowPrice.Value > item.CurrentPrice)
        {
            item.DealLabel = "Buy Now";
            item.DealNote = $"Instant purchase at ${item.BuyNowPrice.Value:N0}";
            return;
        }

        if (item.BidCount <= 1 && item.CurrentPrice <= item.StartingPrice * 1.08m)
        {
            if (item.CurrentPrice >= 3000m || item.BidCount == 0)
            {
                item.DealLabel = "Great Deal";
                item.DealNote = item.BidCount == 0
                    ? "No offers yet"
                    : $"${Math.Round(item.CurrentPrice * 0.9m):N0} offer being considered";
                return;
            }

            item.DealLabel = "Good Deal";
            item.DealNote = "Offers being negotiated";
        }
    }

    public static string BuildListingTitle(AuctionItemViewModel item)
    {
        var parts = new List<string>();
        if (item.Year > 0)
        {
            parts.Add(item.Year.ToString());
        }

        if (!string.IsNullOrWhiteSpace(item.Name))
        {
            parts.Add(item.Name);
        }

        if (!string.IsNullOrWhiteSpace(item.Grade))
        {
            parts.Add(item.Grade);
        }

        return parts.Count > 0 ? string.Join(' ', parts) : item.Name;
    }

    public static bool IsRecommendedDeal(AuctionItemViewModel item) =>
        item.DealLabel is "Great Deal" or "Good Deal";

    public static List<CategoryViewModel> MapCategories(IReadOnlyList<AuctionItemViewModel> items) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item.Category))
            .GroupBy(item => item.Category)
            .OrderBy(group => group.Key)
            .Select(group => new CategoryViewModel
            {
                Name = group.Key,
                ItemCount = group.Count(),
                ImageUrl = CategoryImages.GetImageUrl(group.Key),
                DisplayCount = $"{group.Count()} Items"
            })
            .ToList();

    public const int BidHistoryPreviewLimit = 10;

    public static List<BidHistoryItemViewModel> MapBidHistory(
        IEnumerable<Bid> bids,
        bool revealBidderIdentity = false,
        int? take = null,
        Bid? winningBid = null)
    {
        var allBids = bids.ToList();
        winningBid ??= allBids.FirstOrDefault(bid => bid.IsWinning);

        var bidList = allBids
            .OrderByDescending(bid => bid.PlacedAt)
            .ToList();

        if (take is > 0)
        {
            bidList = bidList.Take(take.Value).ToList();
        }

        return bidList
            .Select(bid => new BidHistoryItemViewModel
            {
                BidderId = revealBidderIdentity ? bid.BidderId : 0,
                BidderName = FormatBidderName(bid.Bidder, revealBidderIdentity),
                Amount = bid.Amount,
                BidTime = bid.PlacedAt,
                Status = ResolveBidStatus(bid, winningBid),
                IsBidderProfilePublic = revealBidderIdentity && bid.BidderId > 0
            })
            .ToList();
    }

    /// <summary>
    /// Bidder names stay private while the live session can still accept bids.
    /// After the live window ends, identities are revealed publicly.
    /// </summary>
    public static bool ShouldRevealBidderIdentity(Auction auction)
    {
        if (auction.Status is AuctionStatuses.Ended
            or AuctionStatuses.AwaitingPayment
            or AuctionStatuses.Completed
            or AuctionStatuses.Cancelled)
        {
            return true;
        }

        return !DateTimeUtilities.IsInFutureUtc(auction.EndDate);
    }

    private static string ResolveBidStatus(Bid bid, Bid? winningBid)
    {
        if (bid.IsWinning)
        {
            return "WINNING";
        }

        if (winningBid is not null && bid.BidderId == winningBid.BidderId)
        {
            return "RAISED";
        }

        return "OUTBID";
    }

    private static string FormatBidderName(ApplicationUser bidder, bool revealFullName)
    {
        var display = string.IsNullOrWhiteSpace(bidder.FullName)
            ? bidder.UserName ?? "Bidder"
            : bidder.FullName.Trim();

        if (revealFullName)
        {
            return display;
        }

        return MaskBidderDisplayName(display);
    }

    internal static string MaskBidderDisplayName(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
        {
            return "Bidder";
        }

        var parts = display
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "Bidder";
        }

        if (parts.Length == 1)
        {
            var single = parts[0];
            if (single.Length <= 2)
            {
                return single;
            }

            return $"{single[0]}***{single[^1]}";
        }

        var first = parts[0];
        var last = parts[^1];
        var visiblePrefixLength = Math.Min(4, Math.Max(1, first.Length - 1));
        var maskedFirst = first[..visiblePrefixLength] + "***";
        return $"{maskedFirst} {last}";
    }

    private static GradingScoreViewModel BuildGrading(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.GradingCentering) ||
            !string.IsNullOrWhiteSpace(product.GradingCorners) ||
            !string.IsNullOrWhiteSpace(product.GradingEdges) ||
            !string.IsNullOrWhiteSpace(product.GradingSurface))
        {
            return new GradingScoreViewModel
            {
                Centering = product.GradingCentering ?? "—",
                Corners = product.GradingCorners ?? "—",
                Edges = product.GradingEdges ?? "—",
                Surface = product.GradingSurface ?? "—"
            };
        }

        return BuildGradingFromGradeLabel(product.GradeLabel);
    }

    private static GradingScoreViewModel BuildGradingFromGradeLabel(string? gradeLabel)
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

    private static List<string> BuildImageGallery(Product product)
    {
        var images = new List<string>();

        if (!string.IsNullOrWhiteSpace(product.PrimaryImage))
        {
            images.Add(ResolveImageUrl(product.PrimaryImage));
        }

        foreach (var image in product.Images.OrderBy(item => item.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(image.ImageUrl))
            {
                continue;
            }

            var url = ResolveImageUrl(image.ImageUrl);
            if (!images.Contains(url))
            {
                images.Add(url);
            }
        }

        return images.Count > 0 ? images : [DefaultProductImageUrl];
    }

    private static List<ProductDocumentViewModel> MapDocuments(Product product) =>
        product.Documents
            .Where(document => document.DeletedAt == null)
            .OrderBy(document => document.Name)
            .Select(document => new ProductDocumentViewModel
            {
                Id = document.Id,
                Name = document.Name,
                FileName = document.Name,
                FileType = document.FileType,
                FileUrl = document.FileUrl,
                ShowCertificateNumber = document.Name.Contains("certificate", StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

    private static List<decimal> BuildQuickBidAmounts(decimal currentPrice, decimal bidStep) =>
    [
        currentPrice + bidStep,
        currentPrice + bidStep * 2,
        currentPrice + bidStep * 5
    ];

    private static (int Days, int Hours, int Minutes, int Seconds) CalculateCountdown(DateTime endDate)
    {
        var remaining = DateTimeUtilities.RemainingUtc(endDate);
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
        AuctionScheduleHelper.IsLiveOpen(auction);

    private static (string Status, string BadgeClass) MapAuctionStatus(string status, DateTime endDate)
    {
        if (!DateTimeUtilities.IsInFutureUtc(endDate) &&
            status is not AuctionStatuses.Cancelled)
        {
            return ("Ended", "bg-stone-600 text-white");
        }

        return status switch
        {
            _ when AuctionStatuses.IsConfirming(status) => ("Confirming", "bg-amber-500 text-white"),
            AuctionStatuses.Rejected => ("Rejected", "bg-red-600 text-white"),
            AuctionStatuses.Scheduled => ("Scheduled", "bg-sky-600 text-white"),
            AuctionStatuses.Live => ("Active Auction", "bg-emerald-600 text-white"),
            AuctionStatuses.EndingSoon => ("Ending Soon", "bg-orange-600 text-white"),
            AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment => ("Ended", "bg-stone-600 text-white"),
            AuctionStatuses.Completed => ("Completed", "bg-stone-600 text-white"),
            AuctionStatuses.Cancelled => ("Cancelled", "bg-stone-500 text-white"),
            _ => ("Active Auction", "bg-emerald-600 text-white")
        };
    }

    public static string MapCardStatus(Auction auction)
    {
        if (!DateTimeUtilities.IsInFutureUtc(auction.EndDate))
        {
            return "Ended";
        }

        if (auction.Status is AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment)
        {
            return "Ended";
        }

        if (auction.Status == AuctionStatuses.Completed)
        {
            return "Completed";
        }

        if (auction.Status == AuctionStatuses.Cancelled)
        {
            return "Cancelled";
        }

        if (AuctionStatuses.IsConfirming(auction.Status))
        {
            return "Confirming";
        }

        if (auction.Status == AuctionStatuses.Rejected)
        {
            return "Rejected";
        }

        if (auction.Status == AuctionStatuses.Scheduled)
        {
            return "Scheduled";
        }

        if (auction.Status == AuctionStatuses.EndingSoon)
        {
            return "Ending Soon";
        }

        var remaining = DateTimeUtilities.RemainingUtc(auction.EndDate);
        if (remaining.TotalHours <= 24)
        {
            return "Ending Soon";
        }

        return "Live";
    }

    private static string ResolveImageUrl(string? primaryImage)
    {
        if (string.IsNullOrWhiteSpace(primaryImage))
        {
            return DefaultProductImageUrl;
        }

        // Older uploads used Cloudinary c_fill (square crop). Rewrite to c_limit
        // so card/list views show the full uploaded image like the sell preview.
        return primaryImage
            .Replace("c_fill,", "c_limit,", StringComparison.OrdinalIgnoreCase)
            .Replace(",c_fill", ",c_limit", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTimeRemaining(DateTime endDate)
    {
        var remaining = DateTimeUtilities.RemainingUtc(endDate);
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
        if (!string.IsNullOrWhiteSpace(product.Subtitle))
        {
            return product.Subtitle.Trim();
        }

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

    private static string ResolveAuthenticator(string? gradeLabel)
    {
        if (string.IsNullOrWhiteSpace(gradeLabel))
        {
            return string.Empty;
        }

        var normalized = gradeLabel.ToUpperInvariant();
        if (normalized.Contains("PSA/DNA", StringComparison.Ordinal))
        {
            return "PSA/DNA";
        }

        if (normalized.Contains("BGS", StringComparison.Ordinal))
        {
            return "BGS";
        }

        if (normalized.Contains("CGC", StringComparison.Ordinal))
        {
            return "CGC";
        }

        if (normalized.Contains("SGC", StringComparison.Ordinal))
        {
            return "SGC";
        }

        if (normalized.Contains("PSA", StringComparison.Ordinal))
        {
            return "PSA";
        }

        return string.Empty;
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
