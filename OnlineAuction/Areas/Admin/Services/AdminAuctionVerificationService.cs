using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.AuctionVerification;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminAuctionVerificationService : IAdminAuctionVerificationService
{
    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private static readonly string[] PlaceholderImageUrls =
    [
        DefaultProductImageUrl,
        "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=600&h=750&fit=crop"
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<AdminAuctionVerificationService> _logger;

    public AdminAuctionVerificationService(
        AuctionHouseDbContext dbContext,
        INotificationService notificationService,
        INotificationLocalizer notifyLocalizer,
        ILogger<AdminAuctionVerificationService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _notifyLocalizer = notifyLocalizer;
        _logger = logger;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Auctions.AsNoTracking()
            .CountAsync(
                auction => auction.DeletedAt == null
                           && auction.Product.DeletedAt == null
                           && auction.Status == AuctionStatuses.PendingReview,
                cancellationToken);

    public async Task<AuctionVerificationListViewModel> GetPendingVerificationsAsync(
        AuctionVerificationFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.DeletedAt == null
                && auction.Product.DeletedAt == null
                && auction.Status == AuctionStatuses.PendingReview);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(auction =>
                auction.Product.Name.Contains(keyword)
                || auction.Product.Category.Name.Contains(keyword)
                || auction.Product.Seller.FullName.Contains(keyword)
                || (auction.Product.Seller.Email != null && auction.Product.Seller.Email.Contains(keyword)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(auction => auction.Product.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ListingType))
        {
            query = query.Where(auction => auction.ListingType == filter.ListingType);
        }

        var dateRange = AdminDateRangeHelper.Parse(filter.DateRange);
        if (dateRange.StartDate.HasValue)
        {
            query = query.Where(auction => auction.SubmittedAt >= dateRange.StartDate.Value);
        }

        if (dateRange.EndDateExclusive.HasValue)
        {
            query = query.Where(auction => auction.SubmittedAt < dateRange.EndDateExclusive.Value);
        }

        query = query.OrderByDescending(auction => auction.SubmittedAt ?? auction.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(auction => new AuctionVerificationListItemViewModel
            {
                Id = auction.Id,
                ProductName = auction.Product.Name,
                SellerName = auction.Product.Seller.FullName,
                CategoryName = auction.Product.Category.Name,
                StartingPrice = auction.StartingPrice,
                SubmittedAt = auction.SubmittedAt,
                ListingType = auction.ListingType,
                ImageUrl = auction.Product.PrimaryImage
            })
            .ToListAsync(cancellationToken);

        return new AuctionVerificationListViewModel
        {
            Items = items,
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId, cancellationToken),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<AuctionVerificationDetailViewModel?> GetVerificationDetailAsync(
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .ThenInclude(p => p.Category)
            .Include(a => a.Product)
            .ThenInclude(p => p.Seller)
            .Include(a => a.Product)
            .ThenInclude(p => p.Images)
            .Include(a => a.Product)
            .ThenInclude(p => p.Documents)
            .FirstOrDefaultAsync(
                a => a.Id == auctionId
                     && a.DeletedAt == null
                     && a.Product.DeletedAt == null
                     && a.Status == AuctionStatuses.PendingReview,
                cancellationToken);

        if (auction is null)
        {
            return null;
        }

        var product = auction.Product;

        return new AuctionVerificationDetailViewModel
        {
            Id = auction.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            ShortDescription = product.ShortDescription,
            DescriptionHtml = product.DescriptionHtml,
            CategoryName = product.Category.Name,
            Condition = product.Condition,
            GradeLabel = product.GradeLabel,
            CertNumber = product.CertNumber,
            Language = product.Language,
            CardNumber = product.CardNumber,
            Year = product.Year,
            SetName = product.SetName,
            GradingCentering = product.GradingCentering,
            GradingCorners = product.GradingCorners,
            GradingEdges = product.GradingEdges,
            GradingSurface = product.GradingSurface,
            PrimaryImage = product.PrimaryImage,
            GalleryImages = product.Images
                .Where(image => image.DeletedAt == null)
                .OrderBy(image => image.SortOrder)
                .Select(image => image.ImageUrl)
                .ToList(),
            Documents = product.Documents
                .Where(document => document.DeletedAt == null)
                .OrderBy(document => document.Name)
                .Select(document => new VerificationDocumentViewModel
                {
                    Id = document.Id,
                    Name = document.Name,
                    FileUrl = document.FileUrl,
                    FileType = document.FileType,
                    CreatedAt = document.CreatedAt
                })
                .ToList(),
            StartingPrice = auction.StartingPrice,
            BidStep = auction.BidStep,
            BuyNowPrice = auction.BuyNowPrice,
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            AuctionEventName = auction.AuctionEventName,
            RequiresRegistration = auction.RequiresRegistration,
            ListingType = auction.ListingType,
            Status = auction.Status,
            SubmittedAt = auction.SubmittedAt,
            SellerId = product.SellerId,
            SellerName = product.Seller.FullName,
            SellerEmail = product.Seller.Email ?? string.Empty
        };
    }

    public async Task<(bool Success, string Message)> ApproveAsync(
        int auctionId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(
                a => a.Id == auctionId && a.DeletedAt == null && a.Product.DeletedAt == null,
                cancellationToken);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        if (auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon or AuctionStatuses.Scheduled)
        {
            return (true, "This auction is already approved and active.");
        }

        if (auction.Status != AuctionStatuses.PendingReview)
        {
            return (false, "Only auctions pending review can be approved.");
        }

        var validationError = ValidateForApproval(auction);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;
            auction.Status = auction.StartDate <= now && auction.EndDate > now
                ? AuctionStatuses.Live
                : AuctionStatuses.Scheduled;
            auction.VerifiedAt = now;
            auction.VerifiedBy = adminUserId;
            auction.RejectReason = null;
            auction.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        LogAudit(adminUserId, "approve", auctionId);

        await NotifySellerAsync(
            auction,
            _notifyLocalizer[NotificationKeys.ListingApprovedTitle],
            _notifyLocalizer[NotificationKeys.ListingApprovedMessage],
            "/Account/Selling?tab=active",
            NotificationReferenceTypes.AuctionNowLive,
            cancellationToken);

        var statusMessage = auction.Status == AuctionStatuses.Scheduled
            ? "Auction approved and scheduled to go live at the start date."
            : "Auction approved and is now live.";

        return (true, statusMessage);
    }

    public async Task<(bool Success, string Message)> RejectAsync(
        int auctionId,
        int adminUserId,
        string rejectReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rejectReason) || rejectReason.Trim().Length < 10)
        {
            return (false, "Reject reason must be at least 10 characters.");
        }

        var auction = await _dbContext.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(
                a => a.Id == auctionId && a.DeletedAt == null && a.Product.DeletedAt == null,
                cancellationToken);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        if (auction.Status == AuctionStatuses.Rejected)
        {
            return (true, "This auction is already rejected.");
        }

        if (auction.Status != AuctionStatuses.PendingReview)
        {
            return (false, "Only auctions pending review can be rejected.");
        }

        var now = DateTime.UtcNow;
        auction.Status = AuctionStatuses.Rejected;
        auction.RejectReason = rejectReason.Trim();
        auction.VerifiedAt = now;
        auction.VerifiedBy = adminUserId;
        auction.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        LogAudit(adminUserId, "reject", auctionId, rejectReason.Trim());

        await NotifySellerAsync(
            auction,
            _notifyLocalizer[NotificationKeys.ListingRejectedTitle],
            _notifyLocalizer.Format(NotificationKeys.ListingRejectedMessage, rejectReason.Trim()),
            "/Account/Selling?tab=active",
            referenceType: null,
            cancellationToken);

        return (true, "Auction rejected successfully.");
    }

    public async Task<int> ActivateScheduledAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var scheduledAuctions = await _dbContext.Auctions
            .Include(a => a.Product)
            .Where(auction =>
                auction.DeletedAt == null
                && auction.Status == AuctionStatuses.Scheduled
                && auction.StartDate <= now
                && auction.EndDate > now)
            .ToListAsync(cancellationToken);

        if (scheduledAuctions.Count == 0)
        {
            return 0;
        }

        foreach (var auction in scheduledAuctions)
        {
            auction.Status = AuctionStatuses.Live;
            auction.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var auction in scheduledAuctions)
        {
            var registrantIds = await _dbContext.AuctionRegistrations
                .AsNoTracking()
                .Where(r => r.AuctionId == auction.Id && r.Status == AuctionRegistrationStatuses.Approved)
                .Select(r => r.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (registrantIds.Count == 0)
            {
                continue;
            }

            var productName = auction.Product?.Name ?? "an auction";
            var relatedUrl = $"/Auction/Detail/{auction.Id}";

            foreach (var userId in registrantIds)
            {
                await _notificationService.CreateAndPushAsync(
                    userId,
                    _notifyLocalizer[NotificationKeys.AuctionNowLiveTitle],
                    _notifyLocalizer.Format(NotificationKeys.AuctionNowLiveMessage, productName),
                    NotificationType.Auction,
                    relatedUrl,
                    NotificationReferenceTypes.AuctionNowLive,
                    auction.Id,
                    cancellationToken: cancellationToken);
            }
        }

        return scheduledAuctions.Count;
    }

    private static string? ValidateForApproval(Auction auction)
    {
        var scheduleError = AuctionScheduleHelper.ValidateSchedule(
            auction.RegistrationStartDate,
            auction.RegistrationEndDate,
            auction.StartDate,
            auction.EndDate);

        if (scheduleError is not null)
        {
            return scheduleError;
        }

        if (auction.StartingPrice <= 0)
        {
            return "Starting price must be greater than 0.";
        }

        if (auction.ListingType == ListingTypes.Auction && auction.BidStep <= 0)
        {
            return "Bid step must be greater than 0.";
        }

        if (IsPlaceholderImage(auction.Product.PrimaryImage))
        {
            return "Product must have a real primary image before approval.";
        }

        if (string.IsNullOrWhiteSpace(auction.Product.ShortDescription)
            && string.IsNullOrWhiteSpace(auction.Product.DescriptionHtml))
        {
            return "Product must have a description or short description.";
        }

        return null;
    }

    private static bool IsPlaceholderImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true;
        }

        return PlaceholderImageUrls.Any(placeholder =>
            string.Equals(placeholder, imageUrl, StringComparison.OrdinalIgnoreCase));
    }

    private void LogAudit(int adminUserId, string action, int auctionId, string? details = null)
    {
        _logger.LogInformation(
            "Auction verification audit: AdminId={AdminId}, Action={Action}, AuctionId={AuctionId}, Timestamp={Timestamp}, Details={Details}",
            adminUserId,
            action,
            auctionId,
            DateTime.UtcNow,
            details);
    }

    private async Task NotifySellerAsync(
        Auction auction,
        string title,
        string message,
        string relatedUrl,
        string? referenceType,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAndPushAsync(
                auction.Product.SellerId,
                title,
                message,
                NotificationType.Auction,
                relatedUrl,
                referenceType ?? "auction",
                auction.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify seller {SellerId} for auction {AuctionId}.", auction.Product.SellerId, auction.Id);
        }
    }

    private async Task<List<SelectListItem>> BuildCategoryOptionsAsync(
        int? selectedId,
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.DeletedAt == null && category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);

        return categories
            .Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Name,
                Selected = selectedId == category.Id
            })
            .ToList();
    }

    private static void NormalizeFilter(AuctionVerificationFilterViewModel filter)
    {
        if (filter.Page <= 0)
        {
            filter.Page = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        filter.PageSize = Math.Min(filter.PageSize, 50);
    }
}
