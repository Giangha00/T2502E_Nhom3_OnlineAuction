using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.BuyNow;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public sealed class AdminBuyNowService : IAdminBuyNowService
{
    private const string ProductImageFolder = "auction-house/products";

    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private static readonly string[] AllowedStatuses =
    [
        AuctionStatuses.PendingReview,
        AuctionStatuses.Rejected,
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Ended,
        AuctionStatuses.AwaitingPayment,
        AuctionStatuses.Completed,
        AuctionStatuses.Cancelled
    ];

    private static readonly HashSet<string> EditableStatuses =
    [
        AuctionStatuses.PendingReview,
        AuctionStatuses.Rejected,
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private static readonly HashSet<string> CancellableStatuses =
    [
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPhotoService _photoService;
    private readonly ILogger<AdminBuyNowService> _logger;

    public AdminBuyNowService(
        AuctionHouseDbContext dbContext,
        IPhotoService photoService,
        ILogger<AdminBuyNowService> logger)
    {
        _dbContext = dbContext;
        _photoService = photoService;
        _logger = logger;
    }

    public async Task<BuyNowListViewModel> GetListingsAsync(BuyNowFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var now = DateTime.UtcNow;
        var query = _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.DeletedAt == null &&
                auction.Product.DeletedAt == null &&
                auction.ListingType == ListingTypes.BuyNow);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(auction =>
                auction.Product.Name.Contains(keyword) ||
                auction.Product.Category.Name.Contains(keyword) ||
                auction.Product.Seller.FullName.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(auction => auction.Status == filter.Status);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(auction => auction.Product.CategoryId == filter.CategoryId.Value);
        }

        if (filter.SellerId.HasValue)
        {
            query = query.Where(auction => auction.Product.SellerId == filter.SellerId.Value);
        }

        query = filter.SortOrder switch
        {
            "price_asc" => query.OrderBy(auction => auction.BuyNowPrice ?? auction.CurrentPrice),
            "price_desc" => query.OrderByDescending(auction => auction.BuyNowPrice ?? auction.CurrentPrice),
            "name_asc" => query.OrderBy(auction => auction.Product.Name),
            "name_desc" => query.OrderByDescending(auction => auction.Product.Name),
            "date_asc" => query.OrderBy(auction => auction.CreatedAt),
            _ => query.OrderByDescending(auction => auction.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var listings = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(auction => new
            {
                auction.Id,
                auction.Product.Name,
                CategoryName = auction.Product.Category.Name,
                SellerName = auction.Product.Seller.FullName,
                BuyNowPrice = auction.BuyNowPrice ?? auction.CurrentPrice,
                auction.Status,
                auction.Product.PrimaryImage,
                auction.CreatedAt,
                auction.VerifiedAt,
                auction.EndDate
            })
            .ToListAsync();

        return new BuyNowListViewModel
        {
            Listings = listings
                .Select(item =>
                {
                    var isPublicLive = IsPublicLive(item.Status, item.BuyNowPrice, item.EndDate, now);
                    return new BuyNowListItemViewModel
                    {
                        Id = item.Id,
                        ProductName = item.Name,
                        CategoryName = item.CategoryName,
                        SellerName = item.SellerName,
                        BuyNowPrice = item.BuyNowPrice,
                        Status = item.Status,
                        AvailabilityLabel = BuildAvailabilityLabel(item.Status, isPublicLive),
                        ImageUrl = item.PrimaryImage,
                        CreatedAt = item.CreatedAt,
                        VerifiedAt = item.VerifiedAt,
                        IsPublicLive = isPublicLive,
                        CanEdit = EditableStatuses.Contains(item.Status)
                    };
                })
                .ToList(),
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId),
            SellerOptions = await BuildSellerOptionsAsync(filter.SellerId),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<BuyNowDetailViewModel?> GetDetailsAsync(int id)
    {
        var now = DateTime.UtcNow;
        var listing = await _dbContext.Auctions
            .AsNoTracking()
            .Where(item =>
                item.Id == id &&
                item.DeletedAt == null &&
                item.Product.DeletedAt == null &&
                item.ListingType == ListingTypes.BuyNow)
            .Select(item => new BuyNowDetailViewModel
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Description = item.Product.DescriptionHtml ?? item.Product.ShortDescription ?? string.Empty,
                CategoryName = item.Product.Category.Name,
                SellerName = item.Product.Seller.FullName,
                SellerEmail = item.Product.Seller.Email ?? string.Empty,
                BuyNowPrice = item.BuyNowPrice ?? item.CurrentPrice,
                StartingPrice = item.StartingPrice,
                Status = item.Status,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                ImageUrl = item.Product.PrimaryImage,
                CreatedAt = item.CreatedAt,
                VerifiedAt = item.VerifiedAt,
                VerifierName = item.Verifier != null ? item.Verifier.FullName : null,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (listing is null)
        {
            return null;
        }

        listing.IsPublicLive = IsPublicLive(listing.Status, listing.BuyNowPrice, listing.EndDate, now);
        listing.AvailabilityLabel = BuildAvailabilityLabel(listing.Status, listing.IsPublicLive);
        listing.CanEdit = EditableStatuses.Contains(listing.Status);
        listing.CanCancel = CancellableStatuses.Contains(listing.Status);
        listing.RelatedOrders = await LoadRelatedOrdersAsync(id);

        return listing;
    }

    public async Task<BuyNowFormViewModel> BuildCreateFormAsync()
    {
        var now = DateTime.UtcNow;
        var liveStart = now.AddMinutes(1);

        return new BuyNowFormViewModel
        {
            StartDate = liveStart,
            EndDate = now.AddYears(1),
            Status = AuctionStatuses.Live,
            CategoryOptions = await BuildCategoryOptionsAsync(),
            SellerOptions = await BuildSellerOptionsAsync(),
            StatusOptions = BuildStatusOptions()
        };
    }

    public async Task<BuyNowFormViewModel?> GetEditFormAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.DeletedAt == null &&
                item.Product.DeletedAt == null &&
                item.ListingType == ListingTypes.BuyNow);

        if (auction is null)
        {
            return null;
        }

        if (!EditableStatuses.Contains(auction.Status))
        {
            return null;
        }

        var hasOrders = await _dbContext.OrderItems
            .AnyAsync(item => item.AuctionId == id && item.DeletedAt == null);

        return new BuyNowFormViewModel
        {
            Id = auction.Id,
            ProductId = auction.ProductId,
            ProductName = auction.Product.Name,
            Description = auction.Product.DescriptionHtml ?? auction.Product.ShortDescription ?? string.Empty,
            BuyNowPrice = auction.BuyNowPrice ?? auction.CurrentPrice,
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            Status = auction.Status,
            CategoryId = auction.Product.CategoryId,
            SellerId = auction.Product.SellerId,
            ImageUrl = auction.Product.PrimaryImage,
            HasOrders = hasOrders,
            CategoryOptions = await BuildCategoryOptionsAsync(auction.Product.CategoryId),
            SellerOptions = await BuildSellerOptionsAsync(auction.Product.SellerId),
            StatusOptions = BuildStatusOptions(auction.Status)
        };
    }

    public async Task PopulateFormOptionsAsync(BuyNowFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.SellerOptions = await BuildSellerOptionsAsync(model.SellerId);
        model.StatusOptions = BuildStatusOptions(model.Status);
    }

    public async Task<(bool Success, string Message)> CreateAsync(BuyNowFormViewModel model)
    {
        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        string? imageUrl;
        try
        {
            imageUrl = await _photoService.AddPhotoAsync(model.ImageFile, ProductImageFolder);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }

        imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? DefaultProductImageUrl : imageUrl;

        var now = DateTime.UtcNow;
        var startingPrice = ResolveStartingPrice(model.BuyNowPrice);
        var product = new Product
        {
            SellerId = model.SellerId,
            CategoryId = model.CategoryId,
            Name = model.ProductName.Trim(),
            ShortDescription = TruncatePlainText(model.Description, 300),
            DescriptionHtml = model.Description.Trim(),
            Condition = "graded",
            PrimaryImage = imageUrl,
            CreatedAt = now
        };

        var auction = new Auction
        {
            Product = product,
            StartingPrice = startingPrice,
            BidStep = 0.01m,
            CurrentPrice = model.BuyNowPrice,
            BuyNowPrice = model.BuyNowPrice,
            ListingType = ListingTypes.BuyNow,
            RequiresRegistration = false,
            Status = model.Status,
            RegistrationStartDate = now,
            RegistrationEndDate = model.StartDate,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            VerifiedAt = model.Status is AuctionStatuses.Live or AuctionStatuses.Scheduled or AuctionStatuses.EndingSoon
                ? now
                : null,
            CreatedAt = now
        };

        _dbContext.Auctions.Add(auction);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Could not create Buy Now listing. Check price, dates, and selected references.");
        }

        _logger.LogInformation("Admin created Buy Now listing {AuctionId}.", auction.Id);
        return (true, "Buy Now listing created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(BuyNowFormViewModel model)
    {
        if (model.Id <= 0)
        {
            return (false, "Listing id is required.");
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item =>
                item.Id == model.Id &&
                item.DeletedAt == null &&
                item.Product.DeletedAt == null &&
                item.ListingType == ListingTypes.BuyNow);

        if (auction is null)
        {
            return (false, "Buy Now listing not found.");
        }

        if (!EditableStatuses.Contains(auction.Status))
        {
            return (false, "This listing cannot be edited in its current status.");
        }

        model.HasOrders = await _dbContext.OrderItems
            .AnyAsync(item => item.AuctionId == model.Id && item.DeletedAt == null);

        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        string? imageUrl = auction.Product.PrimaryImage;
        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            try
            {
                imageUrl = await _photoService.AddPhotoAsync(model.ImageFile, ProductImageFolder);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return (false, "Image upload failed.");
            }
        }

        auction.Product.Name = model.ProductName.Trim();
        auction.Product.ShortDescription = TruncatePlainText(model.Description, 300);
        auction.Product.DescriptionHtml = model.Description.Trim();
        auction.Product.CategoryId = model.CategoryId;
        auction.Product.SellerId = model.SellerId;
        auction.Product.PrimaryImage = imageUrl ?? DefaultProductImageUrl;
        auction.Product.UpdatedAt = DateTime.UtcNow;

        if (!model.HasOrders)
        {
            auction.StartingPrice = ResolveStartingPrice(model.BuyNowPrice);
            auction.CurrentPrice = model.BuyNowPrice;
        }

        auction.BuyNowPrice = model.BuyNowPrice;
        auction.Status = model.Status;
        auction.StartDate = model.StartDate;
        auction.EndDate = model.EndDate;
        auction.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Could not update Buy Now listing. Check price, dates, and selected references.");
        }

        _logger.LogInformation("Admin updated Buy Now listing {AuctionId}.", auction.Id);
        return (true, "Buy Now listing updated successfully.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.DeletedAt == null &&
                item.ListingType == ListingTypes.BuyNow);

        if (auction is null)
        {
            return (false, "Buy Now listing not found.");
        }

        if (!CancellableStatuses.Contains(auction.Status))
        {
            return (false, "Only live or scheduled Buy Now listings can be cancelled.");
        }

        auction.Status = AuctionStatuses.Cancelled;
        auction.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Admin cancelled Buy Now listing {AuctionId}.", auction.Id);
        return (true, "Buy Now listing cancelled successfully.");
    }

    private async Task<IReadOnlyList<BuyNowOrderSummaryViewModel>> LoadRelatedOrdersAsync(int auctionId)
    {
        return await _dbContext.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.AuctionId == auctionId &&
                item.DeletedAt == null &&
                item.Order.DeletedAt == null &&
                item.Order.OrderSource == OrderSources.BuyNow)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new BuyNowOrderSummaryViewModel
            {
                OrderId = item.OrderId,
                OrderReference = item.Order.OrderReference,
                BuyerName = item.Order.Buyer.FullName,
                Status = item.Order.Status,
                TotalAmount = item.Order.TotalAmount,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<string?> ValidateReferencesAsync(BuyNowFormViewModel model)
    {
        if (!AllowedStatuses.Contains(model.Status))
        {
            return "Invalid listing status.";
        }

        if (model.BuyNowPrice <= 0)
        {
            return "Buy Now price must be greater than 0.";
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(category => category.Id == model.CategoryId && category.DeletedAt == null && category.IsActive);

        if (!categoryExists)
        {
            return "Selected category is not available.";
        }

        var sellerExists = await _dbContext.Users
            .AnyAsync(user => user.Id == model.SellerId && user.DeletedAt == null && user.Status == UserStatus.Active);

        if (!sellerExists)
        {
            return "Selected seller is not available.";
        }

        return null;
    }

    private async Task<List<SelectListItem>> BuildCategoryOptionsAsync(int? selectedId = null)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.DeletedAt == null && category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync();

        return categories
            .Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Name,
                Selected = selectedId == category.Id
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> BuildSellerOptionsAsync(int? selectedId = null)
    {
        var sellers = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null && user.Status == UserStatus.Active)
            .OrderBy(user => user.FullName)
            .Select(user => new { user.Id, user.FullName, user.Email })
            .ToListAsync();

        return sellers
            .Select(seller => new SelectListItem
            {
                Value = seller.Id.ToString(),
                Text = $"{seller.FullName} ({seller.Email})",
                Selected = selectedId == seller.Id
            })
            .ToList();
    }

    private static List<SelectListItem> BuildStatusOptions(string? selected = null)
    {
        return AllowedStatuses
            .Select(status => new SelectListItem
            {
                Value = status,
                Text = status.Replace('_', ' '),
                Selected = string.Equals(status, selected, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private static void NormalizeFilter(BuyNowFilterViewModel filter)
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

    private static decimal ResolveStartingPrice(decimal buyNowPrice) =>
        buyNowPrice <= 0.01m ? 0.01m : buyNowPrice - 0.01m;

    private static bool IsPublicLive(string status, decimal buyNowPrice, DateTime endDate, DateTime now) =>
        buyNowPrice > 0 &&
        (status == AuctionStatuses.Live || status == AuctionStatuses.EndingSoon) &&
        endDate > now;

    private static string BuildAvailabilityLabel(string status, bool isPublicLive) =>
        status switch
        {
            AuctionStatuses.Cancelled => "Cancelled",
            AuctionStatuses.Completed => "Sold",
            AuctionStatuses.PendingReview => "Pending review",
            AuctionStatuses.Rejected => "Rejected",
            AuctionStatuses.AwaitingPayment => "Awaiting payment",
            AuctionStatuses.Ended => "Ended",
            AuctionStatuses.Scheduled => "Scheduled",
            AuctionStatuses.Live or AuctionStatuses.EndingSoon =>
                isPublicLive ? "Available" : "Not on public catalog",
            _ => status.Replace('_', ' ')
        };

    private static string TruncatePlainText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var plain = System.Text.RegularExpressions.Regex.Replace(value, "<.*?>", string.Empty).Trim();
        return plain.Length <= maxLength ? plain : plain[..maxLength];
    }
}
