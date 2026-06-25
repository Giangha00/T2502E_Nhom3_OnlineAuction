using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Products;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminProductService : IAdminProductService
{
    private const string ProductImageFolder = "auction-house/products";

    private const string DocumentFolder = "auction-house/documents";

    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private static readonly string[] BlockedDeleteStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.AwaitingPayment
    ];

    private static readonly string[] LockedSellerChangeStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.AwaitingPayment
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPhotoService _photoService;

    public AdminProductService(AuctionHouseDbContext dbContext, IPhotoService photoService)
    {
        _dbContext = dbContext;
        _photoService = photoService;
    }

    public async Task<ProductListViewModel> GetProductsAsync(ProductFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(product =>
                product.Name.Contains(keyword) ||
                (product.CardNumber != null && product.CardNumber.Contains(keyword)) ||
                (product.CertNumber != null && product.CertNumber.Contains(keyword)) ||
                product.Seller.FullName.Contains(keyword));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == filter.CategoryId.Value);
        }

        if (filter.SellerId.HasValue)
        {
            query = query.Where(product => product.SellerId == filter.SellerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Condition))
        {
            query = query.Where(product => product.Condition == filter.Condition);
        }

        var dateRange = ParseDateRange(filter.DateRange);

        if (dateRange.StartDate.HasValue && dateRange.EndDate.HasValue)
        {
            query = query.Where(product =>
                product.CreatedAt >= dateRange.StartDate.Value &&
                product.CreatedAt < dateRange.EndDate.Value);
        }
        else
        {
            if (filter.FromDate.HasValue)
            {
                query = query.Where(product => product.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(product => product.CreatedAt < toDate);
            }
        }

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        query = filter.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(product => product.Name),
            "date_asc" => query.OrderBy(product => product.CreatedAt),
            "date_desc" => query.OrderByDescending(product => product.CreatedAt),
            _ => query.OrderBy(product => product.Name)
        };

        var products = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(product => new ProductListItemViewModel
            {
                Id = product.Id,
                Name = product.Name,
                PrimaryImage = product.PrimaryImage,
                CategoryName = product.Category.Name,
                SellerName = product.Seller.FullName,
                Condition = product.Condition,
                CardNumber = product.CardNumber,
                CertNumber = product.CertNumber,
                ImageCount = product.Images.Count(image => image.DeletedAt == null) + 1,
                AuctionCount = product.Auctions.Count(auction => auction.DeletedAt == null),
                CreatedAt = product.CreatedAt
            })
            .ToListAsync();

        return new ProductListViewModel
        {
            Products = products,
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId),
            SellerOptions = await BuildSellerOptionsAsync(filter.SellerId),
            ConditionOptions = await BuildConditionOptionsAsync(filter.Condition),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ProductDetailViewModel?> GetDetailsAsync(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id && item.DeletedAt == null)
            .Select(item => new ProductDetailViewModel
            {
                Id = item.Id,
                Name = item.Name,
                ShortDescription = item.ShortDescription,
                Subtitle = item.Subtitle,
                DescriptionHtml = item.DescriptionHtml,
                CategoryName = item.Category.Name,
                SellerName = item.Seller.FullName,
                SellerEmail = item.Seller.Email ?? string.Empty,
                Condition = item.Condition,
                ProductOrigin = item.ProductOrigin,
                Year = item.Year,
                SetName = item.SetName,
                Language = item.Language,
                CardNumber = item.CardNumber,
                GradeLabel = item.GradeLabel,
                CertNumber = item.CertNumber,
                Centering = item.GradingCentering,
                Corners = item.GradingCorners,
                Edges = item.GradingEdges,
                Surface = item.GradingSurface,
                EstimatedValue = item.EstimatedValue,
                ImportPrice = item.ImportPrice,
                PrimaryImage = item.PrimaryImage,
                GalleryImages = item.Images
                    .Where(image => image.DeletedAt == null)
                    .OrderBy(image => image.SortOrder)
                    .Select(image => new ProductFormViewModel.GalleryImageItem
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl,
                        SortOrder = image.SortOrder
                    })
                    .ToList(),
                Documents = item.Documents
                    .Where(document => document.DeletedAt == null)
                    .OrderBy(document => document.Name)
                    .Select(document => new ProductFormViewModel.DocumentItem
                    {
                        Id = document.Id,
                        Name = document.Name,
                        FileUrl = document.FileUrl,
                        FileType = document.FileType
                    })
                    .ToList(),
                Auctions = item.Auctions
                    .Where(auction => auction.DeletedAt == null)
                    .OrderByDescending(auction => auction.CreatedAt)
                    .Select(auction => new ProductAuctionItemViewModel
                    {
                        AuctionId = auction.Id,
                        Status = auction.Status,
                        StartingPrice = auction.StartingPrice,
                        CurrentPrice = auction.CurrentPrice,
                        StartDate = auction.StartDate,
                        EndDate = auction.EndDate
                    })
                    .ToList(),
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return product;
    }

    public async Task<ProductFormViewModel> BuildCreateFormAsync()
    {
        var model = new ProductFormViewModel
        {
            Condition = "graded"
        };

        await PopulateFormOptionsAsync(model);
        return model;
    }

    public async Task<ProductFormViewModel?> GetEditFormAsync(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Auctions)
            .Where(item => item.Id == id && item.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return null;
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Subtitle = product.Subtitle,
            DescriptionHtml = product.DescriptionHtml,
            CategoryId = product.CategoryId,
            SellerId = product.SellerId,
            Condition = product.Condition,
            ProductOrigin = product.ProductOrigin,
            Year = product.Year,
            SetName = product.SetName,
            Language = product.Language,
            CardNumber = product.CardNumber,
            GradeLabel = product.GradeLabel,
            CertNumber = product.CertNumber,
            Centering = product.GradingCentering,
            Corners = product.GradingCorners,
            Edges = product.GradingEdges,
            Surface = product.GradingSurface,
            EstimatedValue = product.EstimatedValue,
            ImportPrice = product.ImportPrice,
            PrimaryImageUrl = product.PrimaryImage,
            CanChangeSeller = !HasLockedAuction(product.Auctions),
            ExistingGalleryImages = await _dbContext.ProductImages
                .AsNoTracking()
                .Where(image => image.ProductId == id && image.DeletedAt == null)
                .OrderBy(image => image.SortOrder)
                .Select(image => new ProductFormViewModel.GalleryImageItem
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    SortOrder = image.SortOrder
                })
                .ToListAsync(),
            ExistingDocuments = await _dbContext.ProductDocuments
                .AsNoTracking()
                .Where(document => document.ProductId == id && document.DeletedAt == null)
                .OrderBy(document => document.Name)
                .Select(document => new ProductFormViewModel.DocumentItem
                {
                    Id = document.Id,
                    Name = document.Name,
                    FileUrl = document.FileUrl,
                    FileType = document.FileType
                })
                .ToListAsync()
        };

        await PopulateFormOptionsAsync(model);
        return model;
    }

    public async Task<(bool Success, string Message)> CreateAsync(ProductFormViewModel model, int? createdBy)
    {
        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .Take(4)
            .ToList();

        if (1 + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateCoreAsync(model, galleryFiles, createdBy));
    }

    private async Task<(bool Success, string Message)> CreateCoreAsync(
        ProductFormViewModel model,
        IReadOnlyList<IFormFile> galleryFiles,
        int? createdBy)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            string? imageUrl;
            try
            {
                imageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            imageUrl = string.IsNullOrWhiteSpace(imageUrl)
                ? DefaultProductImageUrl
                : imageUrl;

            var now = DateTime.UtcNow;
            var product = MapToEntity(model, imageUrl, now, createdBy);

            var sortOrder = 1;
            foreach (var galleryFile in galleryFiles)
            {
                try
                {
                    var galleryUrl = await _photoService.AddPhotoAsync(galleryFile, ProductImageFolder);
                    if (string.IsNullOrWhiteSpace(galleryUrl))
                    {
                        continue;
                    }

                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = galleryUrl,
                        SortOrder = sortOrder++,
                        CreatedAt = now,
                        CreatedBy = createdBy
                    });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    return (false, ex.Message);
                }
            }

            for (var i = 0; i < model.DocumentFiles.Count; i++)
            {
                var documentFile = model.DocumentFiles[i];
                if (documentFile is not { Length: > 0 })
                {
                    continue;
                }

                try
                {
                    var documentUrl = await _photoService.AddPhotoAsync(documentFile, DocumentFolder);
                    if (string.IsNullOrWhiteSpace(documentUrl))
                    {
                        continue;
                    }

                    var documentName = i < model.DocumentNames.Count && !string.IsNullOrWhiteSpace(model.DocumentNames[i])
                        ? model.DocumentNames[i].Trim()
                        : documentFile.FileName;

                    product.Documents.Add(new ProductDocument
                    {
                        Name = documentName,
                        FileUrl = documentUrl,
                        FileType = ResolveDocumentType(documentFile),
                        CreatedAt = now,
                        CreatedBy = createdBy
                    });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    return (false, ex.Message);
                }
            }

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Product created successfully.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateAsync(ProductFormViewModel model, int? updatedBy)
    {
        if (!model.Id.HasValue)
        {
            return (false, "Product id is required.");
        }

        var product = await _dbContext.Products
            .Include(item => item.Images)
            .Include(item => item.Documents)
            .Include(item => item.Auctions)
            .FirstOrDefaultAsync(item => item.Id == model.Id.Value && item.DeletedAt == null);

        if (product is null)
        {
            return (false, "Product not found.");
        }

        var canChangeSeller = !HasLockedAuction(product.Auctions);
        if (!canChangeSeller && model.SellerId != product.SellerId)
        {
            return (false, "Cannot change seller while the product has an active or upcoming auction.");
        }

        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .ToList();

        var remainingGalleryCount = product.Images.Count(image =>
            image.DeletedAt == null && !model.RemovedGalleryImageIds.Contains(image.Id));

        if (1 + remainingGalleryCount + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images in total (including the primary image).");
        }

        var now = DateTime.UtcNow;

        string? imageUrl = product.PrimaryImage;
        if (model.PrimaryImageFile is { Length: > 0 })
        {
            try
            {
                imageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return (false, "Primary image upload failed.");
            }
        }

        ApplyMetadata(product, model, imageUrl ?? product.PrimaryImage, now, updatedBy);

        if (canChangeSeller)
        {
            product.SellerId = model.SellerId;
        }

        foreach (var imageId in model.RemovedGalleryImageIds.Distinct())
        {
            var image = product.Images.FirstOrDefault(item => item.Id == imageId && item.DeletedAt == null);
            if (image is not null)
            {
                image.DeletedAt = now;
                image.DeletedBy = updatedBy;
                image.UpdatedAt = now;
            }
        }

        var nextSortOrder = product.Images
            .Where(image => image.DeletedAt == null)
            .Select(image => image.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;

        foreach (var galleryFile in galleryFiles)
        {
            try
            {
                var galleryUrl = await _photoService.AddPhotoAsync(galleryFile, ProductImageFolder);
                if (string.IsNullOrWhiteSpace(galleryUrl))
                {
                    continue;
                }

                product.Images.Add(new ProductImage
                {
                    ImageUrl = galleryUrl,
                    SortOrder = nextSortOrder++,
                    CreatedAt = now,
                    CreatedBy = updatedBy
                });
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        foreach (var documentId in model.RemovedDocumentIds.Distinct())
        {
            var document = product.Documents.FirstOrDefault(item => item.Id == documentId && item.DeletedAt == null);
            if (document is not null)
            {
                document.DeletedAt = now;
                document.DeletedBy = updatedBy;
                document.UpdatedAt = now;
            }
        }

        for (var i = 0; i < model.DocumentFiles.Count; i++)
        {
            var documentFile = model.DocumentFiles[i];
            if (documentFile is not { Length: > 0 })
            {
                continue;
            }

            try
            {
                var documentUrl = await _photoService.AddPhotoAsync(documentFile, DocumentFolder);
                if (string.IsNullOrWhiteSpace(documentUrl))
                {
                    continue;
                }

                var documentName = i < model.DocumentNames.Count && !string.IsNullOrWhiteSpace(model.DocumentNames[i])
                    ? model.DocumentNames[i].Trim()
                    : documentFile.FileName;

                product.Documents.Add(new ProductDocument
                {
                    Name = documentName,
                    FileUrl = documentUrl,
                    FileType = ResolveDocumentType(documentFile),
                    CreatedAt = now,
                    CreatedBy = updatedBy
                });
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Could not update product. Check field values and selected references.");
        }

        return (true, "Product updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int? deletedBy)
    {
        var product = await _dbContext.Products
            .Include(item => item.Auctions)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (product is null)
        {
            return (false, "Product not found.");
        }

        var blockingAuction = product.Auctions
            .FirstOrDefault(auction =>
                auction.DeletedAt == null &&
                BlockedDeleteStatuses.Contains(auction.Status));

        if (blockingAuction is not null)
        {
            return (false, $"Cannot delete this product because auction #{blockingAuction.Id} is {FormatStatusLabel(blockingAuction.Status)}.");
        }

        var now = DateTime.UtcNow;
        product.DeletedAt = now;
        product.DeletedBy = deletedBy;
        product.UpdatedAt = now;
        product.UpdatedBy = deletedBy;

        await _dbContext.SaveChangesAsync();

        return (true, "Product deleted successfully.");
    }

    public async Task PopulateFormOptionsAsync(ProductFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.SellerOptions = await BuildSellerOptionsAsync(model.SellerId);
        model.ConditionOptions = await BuildConditionOptionsAsync(model.Condition);
    }

    private static Product MapToEntity(ProductFormViewModel model, string primaryImage, DateTime now, int? createdBy)
    {
        return new Product
        {
            SellerId = model.SellerId,
            CategoryId = model.CategoryId,
            Name = model.Name.Trim(),
            ShortDescription = TrimOrNull(model.ShortDescription),
            Subtitle = TrimOrNull(model.Subtitle),
            DescriptionHtml = model.DescriptionHtml,
            Condition = model.Condition,
            ProductOrigin = TrimOrNull(model.ProductOrigin),
            Year = model.Year,
            SetName = TrimOrNull(model.SetName),
            Language = TrimOrNull(model.Language),
            CardNumber = TrimOrNull(model.CardNumber),
            GradeLabel = TrimOrNull(model.GradeLabel),
            CertNumber = TrimOrNull(model.CertNumber),
            GradingCentering = TrimOrNull(model.Centering),
            GradingCorners = TrimOrNull(model.Corners),
            GradingEdges = TrimOrNull(model.Edges),
            GradingSurface = TrimOrNull(model.Surface),
            EstimatedValue = model.EstimatedValue,
            ImportPrice = model.ImportPrice,
            PrimaryImage = primaryImage,
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    private static void ApplyMetadata(
        Product product,
        ProductFormViewModel model,
        string primaryImage,
        DateTime now,
        int? updatedBy)
    {
        product.Name = model.Name.Trim();
        product.ShortDescription = TrimOrNull(model.ShortDescription);
        product.Subtitle = TrimOrNull(model.Subtitle);
        product.DescriptionHtml = model.DescriptionHtml;
        product.CategoryId = model.CategoryId;
        product.Condition = model.Condition;
        product.ProductOrigin = TrimOrNull(model.ProductOrigin);
        product.Year = model.Year;
        product.SetName = TrimOrNull(model.SetName);
        product.Language = TrimOrNull(model.Language);
        product.CardNumber = TrimOrNull(model.CardNumber);
        product.GradeLabel = TrimOrNull(model.GradeLabel);
        product.CertNumber = TrimOrNull(model.CertNumber);
        product.GradingCentering = TrimOrNull(model.Centering);
        product.GradingCorners = TrimOrNull(model.Corners);
        product.GradingEdges = TrimOrNull(model.Edges);
        product.GradingSurface = TrimOrNull(model.Surface);
        product.EstimatedValue = model.EstimatedValue;
        product.ImportPrice = model.ImportPrice;
        product.PrimaryImage = primaryImage;
        product.UpdatedAt = now;
        product.UpdatedBy = updatedBy;
    }

    private static bool HasLockedAuction(IEnumerable<Auction> auctions)
    {
        return auctions.Any(auction =>
            auction.DeletedAt == null &&
            (LockedSellerChangeStatuses.Contains(auction.Status) ||
             (auction.Status == AuctionStatuses.Live && DateTimeUtilities.IsInFutureUtc(auction.StartDate))));
    }

    private async Task<string?> ValidateReferencesAsync(ProductFormViewModel model)
    {
        var categoryExists = await _dbContext.Categories
            .AnyAsync(category =>
                category.Id == model.CategoryId &&
                category.DeletedAt == null &&
                category.IsActive);

        if (!categoryExists)
        {
            return "Selected category is not available.";
        }

        var sellerExists = await _dbContext.Users
            .AnyAsync(user =>
                user.Id == model.SellerId &&
                user.DeletedAt == null &&
                user.Status == UserStatus.Active);

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

    private async Task<List<SelectListItem>> BuildConditionOptionsAsync(string? selected = null)
    {
        var dbConditions = await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.DeletedAt == null && product.Condition != null)
            .Select(product => product.Condition)
            .Distinct()
            .ToListAsync();

        var conditions = CreateAuctionMockData.Conditions
            .Concat(dbConditions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(condition => condition)
            .ToList();

        return conditions
            .Select(condition => new SelectListItem
            {
                Value = condition,
                Text = condition,
                Selected = string.Equals(condition, selected, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private static void NormalizeFilter(ProductFilterViewModel filter)
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

    private static string FormatStatusLabel(string status) =>
        status.Replace('_', ' ');

    private static (DateTime? StartDate, DateTime? EndDate) ParseDateRange(string? dateRange)
    {
        if (string.IsNullOrWhiteSpace(dateRange))
        {
            return (null, null);
        }

        var dates = dateRange.Split(" - ", StringSplitOptions.TrimEntries);

        if (dates.Length != 2)
        {
            return (null, null);
        }

        var isStartValid = DateTime.TryParseExact(
            dates[0],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var startDate);

        var isEndValid = DateTime.TryParseExact(
            dates[1],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var endDate);

        if (!isStartValid || !isEndValid)
        {
            return (null, null);
        }

        return (startDate.Date, endDate.Date.AddDays(1));
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ResolveDocumentType(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "PDF",
            ".jpg" or ".jpeg" => "JPG",
            ".png" => "PNG",
            _ => "FILE"
        };
    }
}
