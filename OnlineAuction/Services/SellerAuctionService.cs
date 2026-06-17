using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SellerAuctionService : ISellerAuctionService
{
    private const string ProductImageFolder = "auction-house/products";

    // Anh fallback giup products.primary_image khong bi null khi view hien tai chua gui file len server.
    // Khi view upload anh co name="PrimaryImageFile", Cloudinary URL se thay the gia tri nay.
    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private readonly AuctionHouseDbContext _db;
    private readonly IPhotoService _photoService;

    public SellerAuctionService(
        AuctionHouseDbContext db,
        IPhotoService photoService)
    {
        _db = db;
        _photoService = photoService;
    }

    public async Task<List<AuctionItemViewModel>> GetSellerAuctionsAsync(int sellerId, string? channel = null)
    {
        var normalizedChannel = channel?.ToLowerInvariant();

        var rows = await _db.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.Product.SellerId == sellerId &&
                auction.Status != AuctionStatuses.Cancelled &&
                (normalizedChannel == null ||
                 (normalizedChannel == ListingTypes.BuyNow
                     ? auction.ListingType == ListingTypes.BuyNow
                     : auction.ListingType == ListingTypes.Auction)))
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => new
            {
                auction.Id,
                auction.StartingPrice,
                auction.CurrentPrice,
                auction.Status,
                auction.EndDate,
                auction.ListingType,
                ProductName = auction.Product.Name,
                CategoryName = auction.Product.Category.Name,
                ImageUrl = auction.Product.PrimaryImage,
                Grade = auction.Product.GradeLabel,
                Condition = auction.Product.Condition,
                Year = auction.Product.Year
            })
            .ToListAsync();

        return rows.Select(auction => new AuctionItemViewModel
        {
            Id = auction.Id,
            Name = auction.ProductName,
            Category = auction.CategoryName,
            ImageUrl = auction.ImageUrl,
            StartingPrice = auction.StartingPrice,
            CurrentPrice = auction.CurrentPrice,
            Status = auction.Status,
            Grade = auction.Grade ?? string.Empty,
            Condition = auction.Condition,
            Year = auction.Year ?? 0,
            ListingType = auction.ListingType,
            TimeRemaining = auction.ListingType == ListingTypes.BuyNow
                ? "In stock"
                : FormatAuctionTimeRemaining(auction.EndDate)
        }).ToList();
    }

    public async Task<(bool Success, string Message, int? AuctionId)> CreateAsync(
        CreateAuctionViewModel model,
        int sellerId)
    {
        // Validate lai trong Service vi client/JS co the bi tat hoac bi bypass.
        if (model.EndDate <= model.StartDate)
        {
            return (false, "End date must be greater than start date.", null);
        }

        if (model.StartingPrice <= 0 || model.BidStep <= 0)
        {
            return (false, "Starting price and bid step must be greater than 0.", null);
        }

        string? imageUrl;
        try
        {
            // Neu form gui file len, upload len Cloudinary va luu URL vao MySQL.
            imageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, null);
        }

        imageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? DefaultProductImageUrl
            : imageUrl;

        var category = await GetOrCreateCategoryAsync(model.Category);

        // Insert Product truoc. Product chua thong tin mat hang/card va gan voi seller_id.
        // DB moi dung categories rieng, vi vay service nhan chuoi tu form va map thanh category_id.
        var product = new Product
        {
            SellerId = sellerId,
            Name = model.ProductName.Trim(),
            CategoryId = category.Id,
            ShortDescription = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
            DescriptionHtml = model.ProductDescription,
            Condition = model.Condition,
            Year = model.Year,
            SetName = string.IsNullOrWhiteSpace(model.SetName) ? null : model.SetName.Trim(),
            GradeLabel = string.IsNullOrWhiteSpace(model.Grade) ? null : model.Grade.Trim(),
            CertNumber = string.IsNullOrWhiteSpace(model.CertificateNumber) ? null : model.CertificateNumber.Trim(),
            PrimaryImage = imageUrl,
            Category = category,
            CreatedAt = DateTime.UtcNow
        };

        // Insert Auction sau. Auction chua gia, buoc gia, thoi gian va trang thai dau gia.
        var auction = new Auction
        {
            Product = product,
            StartingPrice = model.StartingPrice,
            BidStep = model.BidStep,
            CurrentPrice = model.StartingPrice,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            ListingType = ListingTypes.Auction,
            Status = AuctionStatuses.Live,
            CreatedAt = DateTime.UtcNow
        };

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();

        return (true, "Auction created successfully.", auction.Id);
    }

    public async Task<(bool Success, string Message, int? ProductId)> CreateBuyNowAsync(
        CreateBuyNowViewModel model,
        int sellerId)
    {
        if (model.Price <= 0)
        {
            return (false, "Price must be greater than 0.", null);
        }

        string? imageUrl;
        try
        {
            imageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, null);
        }

        imageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? DefaultProductImageUrl
            : imageUrl;

        var category = await GetOrCreateCategoryAsync(model.Category);
        var now = DateTime.UtcNow;

        var product = new Product
        {
            SellerId = sellerId,
            Name = model.ProductName.Trim(),
            CategoryId = category.Id,
            ShortDescription = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
            DescriptionHtml = model.ProductDescription,
            Condition = model.Condition,
            Year = model.Year,
            SetName = string.IsNullOrWhiteSpace(model.SetName) ? null : model.SetName.Trim(),
            GradeLabel = string.IsNullOrWhiteSpace(model.Grade) ? null : model.Grade.Trim(),
            CertNumber = string.IsNullOrWhiteSpace(model.CertificateNumber) ? null : model.CertificateNumber.Trim(),
            PrimaryImage = imageUrl,
            Category = category,
            CreatedAt = now
        };

        var auction = new Auction
        {
            Product = product,
            StartingPrice = model.Price,
            BidStep = 0.01m,
            CurrentPrice = model.Price,
            StartDate = now,
            EndDate = now.AddYears(1),
            ListingType = ListingTypes.BuyNow,
            Status = AuctionStatuses.Live,
            CreatedAt = now
        };

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();

        return (true, "Buy now listing created successfully.", product.Id);
    }

    public async Task<SellerAuctionFormViewModel?> GetEditFormAsync(int auctionId, int sellerId)
    {
        // Chi lay auction neu seller dang thao tac dung la chu so huu product.
        var auction = await _db.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .ThenInclude(product => product.Category)
            .FirstOrDefaultAsync(a => a.Id == auctionId && a.Product.SellerId == sellerId);

        if (auction is null)
        {
            return null;
        }

        return new SellerAuctionFormViewModel
        {
            AuctionId = auction.Id,
            ProductName = auction.Product.Name,
            Category = auction.Product.Category.Name,
            ShortDescription = auction.Product.ShortDescription,
            DescriptionHtml = auction.Product.DescriptionHtml,
            Condition = auction.Product.Condition,
            Year = auction.Product.Year,
            SetName = auction.Product.SetName,
            GradeLabel = auction.Product.GradeLabel,
            CertNumber = auction.Product.CertNumber,
            PrimaryImage = auction.Product.PrimaryImage,
            StartingPrice = auction.StartingPrice,
            BidStep = auction.BidStep,
            StartDate = auction.StartDate,
            EndDate = auction.EndDate
        };
    }

    public async Task<(bool Success, string Message)> UpdateAsync(
        SellerAuctionFormViewModel model,
        int sellerId)
    {
        if (!model.AuctionId.HasValue)
        {
            return (false, "Invalid auction.");
        }

        var auction = await _db.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == model.AuctionId && a.Product.SellerId == sellerId);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        var hasBids = await _db.Bids.AnyAsync(b => b.AuctionId == auction.Id);
        if (hasBids)
        {
            return (false, "Cannot edit auction that already has bids.");
        }

        if (auction.Status != AuctionStatuses.Live || auction.EndDate <= DateTime.UtcNow)
        {
            return (false, "Only live auctions can be edited.");
        }

        if (model.EndDate < auction.EndDate)
        {
            return (false, "End date can only be extended, not shortened.");
        }

        string? newImageUrl;
        try
        {
            // Edit cung dung Cloudinary: neu co file moi thi thay cover, khong co thi giu anh cu.
            newImageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }

        var category = await GetOrCreateCategoryAsync(model.Category);

        auction.Product.Name = model.ProductName.Trim();
        auction.Product.CategoryId = category.Id;
        auction.Product.Category = category;
        auction.Product.ShortDescription = model.ShortDescription;
        auction.Product.DescriptionHtml = model.DescriptionHtml;
        auction.Product.Condition = model.Condition;
        auction.Product.Year = model.Year;
        auction.Product.SetName = model.SetName;
        auction.Product.GradeLabel = model.GradeLabel;
        auction.Product.CertNumber = model.CertNumber;
        auction.Product.PrimaryImage = string.IsNullOrWhiteSpace(newImageUrl)
            ? model.PrimaryImage
            : newImageUrl;

        auction.StartingPrice = model.StartingPrice;
        auction.CurrentPrice = model.StartingPrice;
        auction.BidStep = model.BidStep;
        auction.EndDate = model.EndDate;

        await _db.SaveChangesAsync();

        return (true, "Auction updated successfully.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int auctionId, int sellerId)
    {
        var auction = await _db.Auctions
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId && a.Product.SellerId == sellerId);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        var hasBids = await _db.Bids.AnyAsync(b => b.AuctionId == auction.Id);
        if (hasBids)
        {
            return (false, "Cannot cancel auction that already has bids.");
        }

        var hasLockedOrder = await _db.OrderItems.AnyAsync(item =>
            item.AuctionId == auction.Id &&
            (item.Order.Status == OrderStatuses.PendingPayment ||
             item.Order.Status == OrderStatuses.Paid));

        if (hasLockedOrder)
        {
            return (false, "Cannot cancel auction that already has a pending or paid order.");
        }

        if (auction.Status != AuctionStatuses.Live || auction.EndDate <= DateTime.UtcNow)
        {
            return (false, "Only live auctions can be cancelled.");
        }

        // Soft delete: khong xoa row khoi database, chi doi status de con lich su.
        auction.Status = AuctionStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return (true, "Auction cancelled successfully.");
    }

    private static string FormatAuctionTimeRemaining(DateTime endDate)
    {
        var remaining = endDate - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return "Ended";
        }

        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays} days left";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours} hours left";
        }

        return $"{Math.Max(1, (int)remaining.TotalMinutes)} minutes left";
    }

    private async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        var normalizedName = string.IsNullOrWhiteSpace(categoryName)
            ? "Uncategorized"
            : categoryName.Trim();
        var normalizedSlug = BuildSlug(normalizedName);

        var category = await _db.Categories
            .FirstOrDefaultAsync(item => item.Name == normalizedName || item.Slug == normalizedSlug);

        if (category is not null)
        {
            return category;
        }

        // Tao category moi khi form gui len gia tri chua co trong bang categories.
        // Viec nay giup CRUD seller khong bi loi foreign key category_id sau khi team merge schema moi.
        category = new Category
        {
            Name = normalizedName,
            Slug = normalizedSlug,
            IsActive = true,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return category;
    }

    private static string BuildSlug(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join("-", new string(chars)
            .Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(slug) ? "uncategorized" : slug;
    }
}
