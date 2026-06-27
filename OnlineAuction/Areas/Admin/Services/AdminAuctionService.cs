using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Auctions;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminAuctionService
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

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPhotoService _photoService;

    public AdminAuctionService(AuctionHouseDbContext dbContext, IPhotoService photoService)
    {
        _dbContext = dbContext;
        _photoService = photoService;
    }

    public async Task<AuctionListViewModel> GetAuctionsAsync(AuctionFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Auctions
            .AsNoTracking()
            .Where(auction => auction.DeletedAt == null && auction.Product.DeletedAt == null);

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

        query = filter.SortOrder switch
        {
            "price_asc" => query.OrderBy(auction => auction.CurrentPrice),
            "price_desc" => query.OrderByDescending(auction => auction.CurrentPrice),
            "end_asc" => query.OrderBy(auction => auction.EndDate),
            "end_desc" => query.OrderByDescending(auction => auction.EndDate),
            "date_asc" => query.OrderBy(auction => auction.CreatedAt),
            "date_desc" => query.OrderByDescending(auction => auction.CreatedAt),
            "name_desc" => query.OrderByDescending(auction => auction.Product.Name),
            _ => query.OrderByDescending(auction => auction.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var auctions = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(auction => new AuctionListItemViewModel
            {
                Id = auction.Id,
                ProductName = auction.Product.Name,
                CategoryName = auction.Product.Category.Name,
                SellerName = auction.Product.Seller.FullName,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                Status = auction.Status,
                ListingType = auction.ListingType,
                StartDate = auction.StartDate,
                EndDate = auction.EndDate,
                BidCount = auction.Bids.Count(bid => bid.DeletedAt == null),
                ImageUrl = auction.Product.PrimaryImage,
                CreatedAt = auction.CreatedAt
            })
            .ToListAsync();

        return new AuctionListViewModel
        {
            Auctions = auctions,
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<AuctionDetailViewModel?> GetDetailsAsync(int id)
    {
        return await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction => auction.Id == id && auction.DeletedAt == null && auction.Product.DeletedAt == null)
            .Select(auction => new AuctionDetailViewModel
            {
                Id = auction.Id,
                ProductId = auction.ProductId,
                ProductName = auction.Product.Name,
                Description = auction.Product.DescriptionHtml ?? auction.Product.ShortDescription ?? string.Empty,
                CategoryName = auction.Product.Category.Name,
                SellerName = auction.Product.Seller.FullName,
                SellerEmail = auction.Product.Seller.Email ?? string.Empty,
                StartingPrice = auction.StartingPrice,
                BidStep = auction.BidStep,
                CurrentPrice = auction.CurrentPrice,
                BuyNowPrice = auction.BuyNowPrice,
                Status = auction.Status,
                ListingType = auction.ListingType,
                RequiresRegistration = auction.RequiresRegistration,
                StartDate = auction.StartDate,
                EndDate = auction.EndDate,
                ImageUrl = auction.Product.PrimaryImage,
                BidCount = auction.Bids.Count(bid => bid.DeletedAt == null),
                RegistrationCount = auction.Registrations.Count(registration => registration.DeletedAt == null),
                WinnerName = auction.Winner != null ? auction.Winner.FullName : null,
                CreatedAt = auction.CreatedAt,
                UpdatedAt = auction.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AuctionFormViewModel> BuildCreateFormAsync()
    {
        var now = DateTime.Now;

        return new AuctionFormViewModel
        {
            StartDate = now,
            EndDate = now.AddDays(7),
            BidStep = 50,
            Status = AuctionStatuses.Live,
            ListingType = ListingTypes.Auction,
            RequiresRegistration = true,
            CategoryOptions = await BuildCategoryOptionsAsync(),
            SellerOptions = await BuildSellerOptionsAsync(),
            StatusOptions = BuildStatusOptions(),
            ListingTypeOptions = BuildListingTypeOptions()
        };
    }

    public async Task<AuctionFormViewModel?> GetEditFormAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null && item.Product.DeletedAt == null);

        if (auction is null)
        {
            return null;
        }

        return new AuctionFormViewModel
        {
            Id = auction.Id,
            ProductId = auction.ProductId,
            ProductName = auction.Product.Name,
            Description = auction.Product.DescriptionHtml ?? auction.Product.ShortDescription ?? string.Empty,
            StartingPrice = auction.StartingPrice,
            BidStep = auction.BidStep,
            CurrentPrice = auction.CurrentPrice,
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            Status = auction.Status,
            ListingType = auction.ListingType,
            RequiresRegistration = auction.RequiresRegistration,
            CategoryId = auction.Product.CategoryId,
            SellerId = auction.Product.SellerId,
            ImageUrl = auction.Product.PrimaryImage,
            BidCount = await _dbContext.Bids.CountAsync(bid => bid.AuctionId == id && bid.DeletedAt == null),
            CategoryOptions = await BuildCategoryOptionsAsync(auction.Product.CategoryId),
            SellerOptions = await BuildSellerOptionsAsync(auction.Product.SellerId),
            StatusOptions = BuildStatusOptions(auction.Status),
            ListingTypeOptions = BuildListingTypeOptions(auction.ListingType)
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(AuctionFormViewModel model)
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

        imageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? DefaultProductImageUrl
            : imageUrl;

        var now = DateTime.UtcNow;
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
            StartingPrice = model.StartingPrice,
            BidStep = model.BidStep,
            CurrentPrice = model.StartingPrice,
            ListingType = model.ListingType,
            RequiresRegistration = model.RequiresRegistration,
            // Admin-created listings bypass seller review and can go live immediately.
            Status = model.Status,
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
            return (false, "Could not create auction. Check prices, dates, and selected references.");
        }

        return (true, "Auction created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(AuctionFormViewModel model)
    {
        if (model.Id <= 0)
        {
            return (false, "Auction id is required.");
        }

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == model.Id && item.DeletedAt == null && item.Product.DeletedAt == null);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        var bidCount = await _dbContext.Bids.CountAsync(bid => bid.AuctionId == model.Id && bid.DeletedAt == null);
        model.BidCount = bidCount;

        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        if (bidCount > 0 && model.StartingPrice > auction.CurrentPrice)
        {
            return (false, "Starting price cannot exceed the current price when bids exist.");
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

        auction.StartingPrice = model.StartingPrice;
        auction.BidStep = model.BidStep;
        auction.ListingType = model.ListingType;
        auction.RequiresRegistration = model.RequiresRegistration;
        auction.Status = model.Status;
        auction.StartDate = model.StartDate;
        auction.EndDate = model.EndDate;
        auction.UpdatedAt = DateTime.UtcNow;

        if (bidCount == 0)
        {
            auction.CurrentPrice = model.StartingPrice;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Could not update auction. Check prices, dates, and selected references.");
        }

        return (true, "Auction updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
            .Include(item => item.Bids)
            .Include(item => item.OrderItems)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        var activeBidCount = auction.Bids.Count(bid => bid.DeletedAt == null);
        if (activeBidCount > 0)
        {
            return (false, $"Cannot delete this auction because it has {activeBidCount} bid(s). Cancel it instead.");
        }

        var activeOrderCount = auction.OrderItems.Count(item => item.DeletedAt == null);
        if (activeOrderCount > 0)
        {
            return (false, "Cannot delete this auction because it is linked to an order.");
        }

        var now = DateTime.UtcNow;
        auction.DeletedAt = now;
        auction.UpdatedAt = now;
        auction.Product.DeletedAt = now;
        auction.Product.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return (true, "Auction deleted successfully.");
    }

    public async Task PopulateFormOptionsAsync(AuctionFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.SellerOptions = await BuildSellerOptionsAsync(model.SellerId);
        model.StatusOptions = BuildStatusOptions(model.Status);
        model.ListingTypeOptions = BuildListingTypeOptions(model.ListingType);
    }

    private async Task<string?> ValidateReferencesAsync(AuctionFormViewModel model)
    {
        if (!AllowedStatuses.Contains(model.Status))
        {
            return "Invalid auction status.";
        }

        if (model.ListingType is not ListingTypes.Auction and not ListingTypes.BuyNow)
        {
            return "Invalid listing type.";
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
                Text = FormatStatusLabel(status),
                Selected = status == selected
            })
            .ToList();
    }

    private static List<SelectListItem> BuildListingTypeOptions(string? selected = null)
    {
        return
        [
            new SelectListItem
            {
                Value = ListingTypes.Auction,
                Text = "Auction",
                Selected = selected == ListingTypes.Auction
            },
            new SelectListItem
            {
                Value = ListingTypes.BuyNow,
                Text = "Buy Now",
                Selected = selected == ListingTypes.BuyNow
            }
        ];
    }

    private static void NormalizeFilter(AuctionFilterViewModel filter)
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

    private static string FormatStatusLabel(string status)
    {
        return status.Replace(' '.ToString(), ' '.ToString(), StringComparison.Ordinal);
    }

    private static string TruncatePlainText(string value, int maxLength)
    {
        var plainText = value.Trim();

        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        return plainText[..maxLength];
    }
}
