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

    private const int MaxGalleryImages = 4;

    private static readonly string[] BlockDeleteAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.AwaitingPayment
    ];

    private static readonly string[] SellerLockAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPhotoService _photoService;

    public AdminProductService(AuctionHouseDbContext dbContext, IPhotoService photoService)
    {
        _dbContext = dbContext;
        _photoService = photoService;
    }

    public async Task<ProductTemplateListViewModel> GetProductTemplatesAsync(ProductTemplateFilterViewModel filter)
    {
        NormalizeTemplateFilter(filter);

        var query = _dbContext.ProductTemplates
            .AsNoTracking()
            .Where(template => template.DeletedAt == null && template.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = query.Where(template =>
                template.Name.Contains(keyword) ||
                template.Category.Name.Contains(keyword) ||
                (template.SetName != null && template.SetName.Contains(keyword)) ||
                (template.CardNumber != null && template.CardNumber.Contains(keyword)));
        }

        query = filter.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(template => template.Name),
            "count_desc" => query.OrderByDescending(template => template.Products.Count(product => product.DeletedAt == null)),
            "date_desc" => query.OrderByDescending(template =>
                template.Products.Where(product => product.DeletedAt == null).Max(product => (DateTime?)product.CreatedAt) ?? template.UpdatedAt ?? template.CreatedAt),
            _ => query.OrderBy(template => template.Name)
        };

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var templates = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(template => new ProductTemplateListItemViewModel
            {
                Id = template.Id,
                Name = template.Name,
                CategoryName = template.Category.Name,
                SetName = template.SetName,
                CardNumber = template.CardNumber,
                GradeLabel = template.GradeLabel,
                ThumbnailUrl = template.PrimaryImage,
                InstanceCount = template.Products.Count(product => product.DeletedAt == null),
                LastAddedAt = template.Products
                    .Where(product => product.DeletedAt == null)
                    .Max(product => (DateTime?)product.CreatedAt)
            })
            .ToListAsync();

        return new ProductTemplateListViewModel
        {
            Templates = templates,
            Filter = filter,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ProductListViewModel?> GetTemplateInstancesAsync(int templateId, ProductFilterViewModel filter)
    {
        var template = await GetTemplateDetailsAsync(templateId);

        if (template is null)
        {
            return null;
        }

        filter.ProductTemplateId = templateId;
        filter.SortOrder = string.IsNullOrWhiteSpace(filter.SortOrder) ? "price_asc" : filter.SortOrder;
        var model = await GetProductsAsync(filter);
        model.ContextTemplateId = templateId;
        model.ContextTemplateName = template.Name;
        model.ContextTemplate = template;
        model.SellerOptions = await BuildSellerOptionsAsync(filter.SellerId, templateId);
        return model;
    }

    public async Task<ProductTemplateFormViewModel> BuildCreateTemplateFormAsync()
    {
        var model = new ProductTemplateFormViewModel();
        await PopulateTemplateFormOptionsAsync(model);
        return model;
    }

    public async Task<ProductTemplateFormViewModel?> BuildEditTemplateFormAsync(int id)
    {
        var template = await _dbContext.ProductTemplates
            .AsNoTracking()
            .Where(item => item.Id == id && item.DeletedAt == null)
            .Select(item => new ProductTemplateFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                CategoryId = item.CategoryId,
                SetName = item.SetName,
                CardNumber = item.CardNumber,
                GradeLabel = item.GradeLabel,
                Year = item.Year,
                Language = item.Language,
                ShortDescription = item.ShortDescription,
                DescriptionHtml = item.DescriptionHtml,
                PrimaryImageUrl = item.PrimaryImage,
                HasInstances = item.Products.Any(product => product.DeletedAt == null)
            })
            .FirstOrDefaultAsync();

        if (template is null)
        {
            return null;
        }

        await PopulateTemplateFormOptionsAsync(template);
        return template;
    }

    public async Task<(bool Success, string Message)> CreateTemplateAsync(ProductTemplateFormViewModel model)
    {
        var validationError = await ValidateTemplateAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        string? imageUrl;
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
            return (false, "Vui lòng tải ảnh chính cho mẫu sản phẩm.");
        }

        var template = new ProductTemplate
        {
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId,
            SetName = TrimOrNull(model.SetName),
            CardNumber = TrimOrNull(model.CardNumber),
            GradeLabel = TrimOrNull(model.GradeLabel),
            Year = model.Year,
            Language = TrimOrNull(model.Language),
            ShortDescription = TrimOrNull(model.ShortDescription),
            DescriptionHtml = model.DescriptionHtml,
            PrimaryImage = imageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ProductTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        return (true, "Đã tạo mẫu sản phẩm thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateTemplateAsync(ProductTemplateFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return (false, "Thiếu mã mẫu sản phẩm.");
        }

        var template = await _dbContext.ProductTemplates
            .FirstOrDefaultAsync(item => item.Id == model.Id.Value && item.DeletedAt == null);

        if (template is null)
        {
            return (false, "Không tìm thấy mẫu sản phẩm.");
        }

        var validationError = await ValidateTemplateAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        if (model.PrimaryImageFile is { Length: > 0 })
        {
            try
            {
                var imageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    template.PrimaryImage = imageUrl;
                }
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        template.Name = model.Name.Trim();
        template.CategoryId = model.CategoryId;
        template.SetName = TrimOrNull(model.SetName);
        template.CardNumber = TrimOrNull(model.CardNumber);
        template.GradeLabel = TrimOrNull(model.GradeLabel);
        template.Year = model.Year;
        template.Language = TrimOrNull(model.Language);
        template.ShortDescription = TrimOrNull(model.ShortDescription);
        template.DescriptionHtml = model.DescriptionHtml;
        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return (true, "Đã cập nhật mẫu sản phẩm thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteTemplateAsync(int id, int adminUserId)
    {
        var template = await _dbContext.ProductTemplates
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (template is null)
        {
            return (false, "Không tìm thấy mẫu sản phẩm.");
        }

        if (template.Products.Any(product => product.DeletedAt == null))
        {
            return (false, "Không thể xóa mẫu sản phẩm đang có sản phẩm của người bán.");
        }

        var now = DateTime.UtcNow;
        template.DeletedAt = now;
        template.DeletedBy = adminUserId;
        template.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return (true, "Đã xóa mẫu sản phẩm thành công.");
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

            if (TryParseProductCode(keyword, out var productId))
            {
                query = query.Where(product => product.Id == productId);
            }
            else
            {
                query = query.Where(product =>
                    product.Name.Contains(keyword) ||
                    (product.CardNumber != null && product.CardNumber.Contains(keyword)) ||
                    (product.CertNumber != null && product.CertNumber.Contains(keyword)) ||
                    product.Seller.FullName.Contains(keyword) ||
                    product.Id.ToString().Contains(keyword));
            }
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == filter.CategoryId.Value);
        }

        if (filter.ProductTemplateId.HasValue)
        {
            query = query.Where(product => product.ProductTemplateId == filter.ProductTemplateId.Value);
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

        if (filter.MinEstimatedValue.HasValue)
        {
            query = query.Where(product =>
                product.EstimatedValue.HasValue &&
                product.EstimatedValue.Value >= filter.MinEstimatedValue.Value);
        }

        if (filter.MaxEstimatedValue.HasValue)
        {
            query = query.Where(product =>
                product.EstimatedValue.HasValue &&
                product.EstimatedValue.Value <= filter.MaxEstimatedValue.Value);
        }

        query = filter.SortOrder switch
        {
            "name_asc" => query.OrderBy(product => product.Name),
            "name_desc" => query.OrderByDescending(product => product.Name),
            "date_asc" => query.OrderBy(product => product.CreatedAt),
            "date_desc" => query.OrderByDescending(product => product.CreatedAt),
            "price_desc" => query.OrderByDescending(product => product.EstimatedValue ?? product.ImportPrice ?? decimal.MaxValue),
            "seller_asc" => query.OrderBy(product => product.Seller.FullName),
            "price_asc" => query.OrderBy(product => product.EstimatedValue ?? product.ImportPrice ?? decimal.MaxValue),
            _ => query.OrderByDescending(product => product.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var products = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(product => new ProductListItemViewModel
            {
                Id = product.Id,
                Name = product.Name,
                ThumbnailUrl = product.PrimaryImage,
                CategoryName = product.Category.Name,
                SellerId = product.SellerId,
                SellerName = product.Seller.FullName,
                SellerEmail = product.Seller.Email ?? string.Empty,
                Condition = product.Condition,
                GradeLabel = product.GradeLabel,
                CardNumber = product.CardNumber,
                CertNumber = product.CertNumber,
                EstimatedValue = product.EstimatedValue,
                ImportPrice = product.ImportPrice,
                AuctionCount = product.Auctions.Count(auction => auction.DeletedAt == null),
                CanDelete = !product.Auctions.Any(auction =>
                    auction.DeletedAt == null &&
                    (auction.Status == AuctionStatuses.Live ||
                     auction.Status == AuctionStatuses.EndingSoon ||
                     auction.Status == AuctionStatuses.AwaitingPayment)),
                CreatedAt = product.CreatedAt
            })
            .ToListAsync();

        ApplyProductCodes(products);

        return new ProductListViewModel
        {
            Products = products,
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId),
            SellerOptions = await BuildSellerOptionsAsync(filter.SellerId, filter.ProductTemplateId),
            ConditionOptions = BuildConditionOptions(filter.Condition),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ProductDetailViewModel?> GetDetailsAsync(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id && item.DeletedAt == null)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.ShortDescription,
                item.Subtitle,
                item.DescriptionHtml,
                item.CategoryId,
                CategoryName = item.Category.Name,
                item.ProductTemplateId,
                ProductTemplateName = item.ProductTemplate == null ? null : item.ProductTemplate.Name,
                item.SellerId,
                SellerName = item.Seller.FullName,
                SellerEmail = item.Seller.Email,
                item.Condition,
                item.ProductOrigin,
                item.Year,
                item.SetName,
                item.Language,
                item.CardNumber,
                item.GradeLabel,
                item.CertNumber,
                item.GradingCentering,
                item.GradingCorners,
                item.GradingEdges,
                item.GradingSurface,
                item.EstimatedValue,
                item.ImportPrice,
                item.PrimaryImage,
                item.CreatedAt,
                item.UpdatedAt,
                CanDelete = !item.Auctions.Any(auction =>
                    auction.DeletedAt == null &&
                    (auction.Status == AuctionStatuses.Live ||
                     auction.Status == AuctionStatuses.EndingSoon ||
                     auction.Status == AuctionStatuses.AwaitingPayment)),
                GalleryImages = item.Images
                    .Where(image => image.DeletedAt == null)
                    .OrderBy(image => image.SortOrder)
                    .Select(image => new ProductImageItemViewModel
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl,
                        SortOrder = image.SortOrder
                    })
                    .ToList(),
                Documents = item.Documents
                    .Where(document => document.DeletedAt == null)
                    .OrderBy(document => document.Name)
                    .Select(document => new ProductDocumentItemViewModel
                    {
                        Id = document.Id,
                        Name = document.Name,
                        FileUrl = document.FileUrl,
                        FileType = document.FileType,
                        CreatedAt = document.CreatedAt
                    })
                    .ToList(),
                LinkedAuctions = item.Auctions
                    .Where(auction => auction.DeletedAt == null)
                    .OrderByDescending(auction => auction.CreatedAt)
                    .Select(auction => new ProductLinkedAuctionViewModel
                    {
                        Id = auction.Id,
                        Status = auction.Status,
                        StartingPrice = auction.StartingPrice,
                        CurrentPrice = auction.CurrentPrice,
                        StartDate = auction.StartDate,
                        EndDate = auction.EndDate,
                        PublicDetailUrl = auction.Status == AuctionStatuses.Live
                            ? $"/Auction/Detail/{auction.Id}"
                            : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return null;
        }

        return new ProductDetailViewModel
        {
            Id = product.Id,
            ProductCode = ProductDisplayHelper.FormatProductCode(product.Id),
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Subtitle = product.Subtitle,
            DescriptionHtml = product.DescriptionHtml,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            ProductTemplateId = product.ProductTemplateId,
            ProductTemplateName = product.ProductTemplateName,
            SellerId = product.SellerId,
            SellerName = product.SellerName,
            SellerEmail = product.SellerEmail,
            Condition = product.Condition,
            ProductOrigin = product.ProductOrigin,
            Year = product.Year,
            SetName = product.SetName,
            Language = product.Language,
            CardNumber = product.CardNumber,
            GradeLabel = product.GradeLabel,
            CertNumber = product.CertNumber,
            GradingCentering = product.GradingCentering,
            GradingCorners = product.GradingCorners,
            GradingEdges = product.GradingEdges,
            GradingSurface = product.GradingSurface,
            EstimatedValue = product.EstimatedValue,
            ImportPrice = product.ImportPrice,
            PrimaryImage = product.PrimaryImage,
            GalleryImages = product.GalleryImages,
            Documents = product.Documents,
            LinkedAuctions = product.LinkedAuctions,
            CanDelete = product.CanDelete,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<ProductFormViewModel> BuildCreateFormAsync(int? templateId = null)
    {
        var model = new ProductFormViewModel();

        if (templateId.HasValue)
        {
            var template = await _dbContext.ProductTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == templateId.Value && item.DeletedAt == null && item.IsActive);

            if (template is not null)
            {
                ApplyTemplateSnapshot(model, template);
                model.ProductTemplateId = template.Id;
                model.ProductTemplateName = template.Name;
                model.ContextTemplateId = template.Id;
                model.IsTemplateLocked = true;
                model.PrimaryImageUrl = template.PrimaryImage;
            }
        }

        await PopulateFormOptionsAsync(model);
        return model;
    }

    public async Task<ProductFormViewModel?> BuildEditFormAsync(int id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Images)
            .Include(item => item.Documents)
            .Include(item => item.Auctions)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (product is null)
        {
            return null;
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            ProductTemplateId = product.ProductTemplateId,
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
            GradingCentering = product.GradingCentering,
            GradingCorners = product.GradingCorners,
            GradingEdges = product.GradingEdges,
            GradingSurface = product.GradingSurface,
            EstimatedValue = product.EstimatedValue,
            ImportPrice = product.ImportPrice,
            PrimaryImageUrl = product.PrimaryImage,
            ExistingGalleryImages = product.Images
                .Where(image => image.DeletedAt == null)
                .OrderBy(image => image.SortOrder)
                .Select(image => new ProductImageItemViewModel
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    SortOrder = image.SortOrder
                })
                .ToList(),
            ExistingDocuments = product.Documents
                .Where(document => document.DeletedAt == null)
                .OrderBy(document => document.Name)
                .Select(document => new ProductDocumentItemViewModel
                {
                    Id = document.Id,
                    Name = document.Name,
                    FileUrl = document.FileUrl,
                    FileType = document.FileType,
                    CreatedAt = document.CreatedAt
                })
                .ToList(),
            IsSellerLocked = product.Auctions.Any(auction =>
                auction.DeletedAt == null && SellerLockAuctionStatuses.Contains(auction.Status)),
            IsTemplateLocked = product.Auctions.Any(auction =>
                auction.DeletedAt == null && SellerLockAuctionStatuses.Contains(auction.Status)),
            ContextTemplateId = product.ProductTemplateId
        };

        await PopulateFormOptionsAsync(model, product.CategoryId, product.SellerId);
        model.ProductTemplateName = model.ProductTemplateOptions
            .FirstOrDefault(option => option.Value == product.ProductTemplateId?.ToString())?.Text;
        return model;
    }

    public async Task<(bool Success, string Message)> CreateAsync(ProductFormViewModel model)
    {
        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var template = await _dbContext.ProductTemplates
            .AsNoTracking()
            .FirstAsync(item => item.Id == model.ProductTemplateId);
        ApplyTemplateSnapshot(model, template);

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .ToList();

        if (galleryFiles.Count > MaxGalleryImages)
        {
            return (false, $"Chỉ được tải tối đa {MaxGalleryImages} ảnh thư viện cho mỗi sản phẩm.");
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateCoreAsync(model, galleryFiles));
    }

    private async Task<(bool Success, string Message)> CreateCoreAsync(
        ProductFormViewModel model,
        IReadOnlyList<IFormFile> galleryFiles)
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
                ? model.PrimaryImageUrl ?? DefaultProductImageUrl
                : imageUrl;

            var now = DateTime.UtcNow;
            var product = MapProductFields(model, now);
            product.PrimaryImage = imageUrl;

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
                        CreatedAt = now
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
                        CreatedAt = now
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

            return (true, "Đã tạo sản phẩm thành công.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateAsync(ProductFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return (false, "Thiếu mã sản phẩm.");
        }

        var product = await _dbContext.Products
            .Include(item => item.Images)
            .Include(item => item.Documents)
            .Include(item => item.Auctions)
            .FirstOrDefaultAsync(item => item.Id == model.Id.Value && item.DeletedAt == null);

        if (product is null)
        {
            return (false, "Không tìm thấy sản phẩm.");
        }

        var isSellerLocked = product.Auctions.Any(auction =>
            auction.DeletedAt == null && SellerLockAuctionStatuses.Contains(auction.Status));

        if (isSellerLocked && model.SellerId != product.SellerId)
        {
            return (false, "Không thể đổi người bán khi sản phẩm đang có phiên đấu giá đang diễn ra hoặc sắp kết thúc.");
        }

        if (isSellerLocked && model.ProductTemplateId != product.ProductTemplateId)
        {
            return (false, "Không thể đổi mẫu sản phẩm khi sản phẩm đang có phiên đấu giá đang diễn ra hoặc sắp kết thúc.");
        }

        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var template = await _dbContext.ProductTemplates
            .AsNoTracking()
            .FirstAsync(item => item.Id == model.ProductTemplateId);
        ApplyTemplateSnapshot(model, template);

        var newGalleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .ToList();

        var remainingGalleryCount = product.Images.Count(image =>
            image.DeletedAt == null && !model.RemoveGalleryImageIds.Contains(image.Id));

        if (remainingGalleryCount + newGalleryFiles.Count > MaxGalleryImages)
        {
            return (false, $"Thư viện chỉ được có tối đa {MaxGalleryImages} ảnh.");
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation);
        }

        var remainingDocumentCount = product.Documents.Count(document =>
            document.DeletedAt == null && !model.RemoveDocumentIds.Contains(document.Id));
        var newDocumentCount = model.DocumentFiles.Count(file => file is { Length: > 0 });
        if (remainingDocumentCount + newDocumentCount > MaxDocumentsPerProduct)
        {
            return (false, $"Mỗi sản phẩm chỉ được có tối đa {MaxDocumentsPerProduct} tài liệu.");
        }

        string? newImageUrl = null;
        if (model.PrimaryImageFile is { Length: > 0 })
        {
            try
            {
                newImageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        var now = DateTime.UtcNow;

        ApplyProductFields(product, model);
        if (!string.IsNullOrWhiteSpace(newImageUrl))
        {
            product.PrimaryImage = newImageUrl;
        }

        foreach (var image in product.Images.Where(image => model.RemoveGalleryImageIds.Contains(image.Id)))
        {
            image.DeletedAt = now;
            image.UpdatedAt = now;
        }

        var maxSortOrder = product.Images
            .Where(image => image.DeletedAt == null)
            .Select(image => image.SortOrder)
            .DefaultIfEmpty(0)
            .Max();

        foreach (var galleryFile in newGalleryFiles)
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
                    SortOrder = ++maxSortOrder,
                    CreatedAt = now
                });
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        foreach (var document in product.Documents.Where(doc => model.RemoveDocumentIds.Contains(doc.Id)))
        {
            document.DeletedAt = now;
            document.UpdatedAt = now;
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
                    CreatedAt = now
                });
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }

        product.UpdatedAt = now;
        await _dbContext.SaveChangesAsync();

        return (true, "Đã cập nhật sản phẩm thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int adminUserId)
    {
        var product = await _dbContext.Products
            .Include(item => item.Auctions)
            .Include(item => item.Images)
            .Include(item => item.Documents)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (product is null)
        {
            return (false, "Không tìm thấy sản phẩm.");
        }

        var blockingReason = GetDeleteBlockingReason(product);
        if (blockingReason is not null)
        {
            return (false, $"Không thể xóa sản phẩm này vì {blockingReason}");
        }

        SoftDeleteProduct(product, adminUserId);
        await _dbContext.SaveChangesAsync();

        return (true, "Đã xóa sản phẩm thành công.");
    }

    public async Task<(bool Success, string Message)> BulkDeleteAsync(IReadOnlyList<int> productIds, int adminUserId)
    {
        if (productIds.Count == 0)
        {
            return (false, "Vui lòng chọn ít nhất một sản phẩm.");
        }

        var products = await _dbContext.Products
            .Include(item => item.Auctions)
            .Include(item => item.Images)
            .Include(item => item.Documents)
            .Where(item => productIds.Contains(item.Id) && item.DeletedAt == null)
            .ToListAsync();

        if (products.Count == 0)
        {
            return (false, "Không tìm thấy sản phẩm nào.");
        }

        var deletedCount = 0;
        var skippedMessages = new List<string>();

        foreach (var product in products)
        {
            var blockingReason = GetDeleteBlockingReason(product);
            if (blockingReason is not null)
            {
                skippedMessages.Add($"#{product.Id} {product.Name}: {blockingReason}");
                continue;
            }

            SoftDeleteProduct(product, adminUserId);
            deletedCount++;
        }

        if (deletedCount == 0)
        {
            return (false, string.Join(" ", skippedMessages));
        }

        await _dbContext.SaveChangesAsync();

        if (skippedMessages.Count == 0)
        {
            return (true, $"Đã xóa {deletedCount} sản phẩm thành công.");
        }

        return (true, $"Đã xóa {deletedCount} sản phẩm. Bỏ qua {skippedMessages.Count}: {string.Join(" ", skippedMessages)}");
    }

    private static string? GetDeleteBlockingReason(Product product)
    {
        var blockingAuction = product.Auctions
            .Where(auction => auction.DeletedAt == null)
            .FirstOrDefault(auction => BlockDeleteAuctionStatuses.Contains(auction.Status));

        if (blockingAuction is null)
        {
            return null;
        }

        return $"phiên đấu giá #{blockingAuction.Id} đang ở trạng thái {FormatStatusLabel(blockingAuction.Status)}.";
    }

    private static void SoftDeleteProduct(Product product, int adminUserId)
    {
        var now = DateTime.UtcNow;
        product.DeletedAt = now;
        product.DeletedBy = adminUserId;
        product.UpdatedAt = now;

        foreach (var image in product.Images.Where(image => image.DeletedAt == null))
        {
            image.DeletedAt = now;
            image.DeletedBy = adminUserId;
            image.UpdatedAt = now;
        }

        foreach (var document in product.Documents.Where(document => document.DeletedAt == null))
        {
            document.DeletedAt = now;
            document.DeletedBy = adminUserId;
            document.UpdatedAt = now;
        }
    }

    private static Product MapProductFields(ProductFormViewModel model, DateTime now)
    {
        return new Product
        {
            SellerId = model.SellerId,
            CategoryId = model.CategoryId,
            ProductTemplateId = model.ProductTemplateId,
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
            GradingCentering = TrimOrNull(model.GradingCentering),
            GradingCorners = TrimOrNull(model.GradingCorners),
            GradingEdges = TrimOrNull(model.GradingEdges),
            GradingSurface = TrimOrNull(model.GradingSurface),
            EstimatedValue = model.EstimatedValue,
            ImportPrice = model.ImportPrice,
            CreatedAt = now
        };
    }

    private static void ApplyProductFields(Product product, ProductFormViewModel model)
    {
        product.SellerId = model.SellerId;
        product.CategoryId = model.CategoryId;
        product.ProductTemplateId = model.ProductTemplateId;
        product.Name = model.Name.Trim();
        product.ShortDescription = TrimOrNull(model.ShortDescription);
        product.Subtitle = TrimOrNull(model.Subtitle);
        product.DescriptionHtml = model.DescriptionHtml;
        product.Condition = model.Condition;
        product.ProductOrigin = TrimOrNull(model.ProductOrigin);
        product.Year = model.Year;
        product.SetName = TrimOrNull(model.SetName);
        product.Language = TrimOrNull(model.Language);
        product.CardNumber = TrimOrNull(model.CardNumber);
        product.GradeLabel = TrimOrNull(model.GradeLabel);
        product.CertNumber = TrimOrNull(model.CertNumber);
        product.GradingCentering = TrimOrNull(model.GradingCentering);
        product.GradingCorners = TrimOrNull(model.GradingCorners);
        product.GradingEdges = TrimOrNull(model.GradingEdges);
        product.GradingSurface = TrimOrNull(model.GradingSurface);
        product.EstimatedValue = model.EstimatedValue;
        product.ImportPrice = model.ImportPrice;
    }

    private async Task<string?> ValidateReferencesAsync(ProductFormViewModel model)
    {
        if (!model.ProductTemplateId.HasValue)
        {
            return "Mẫu sản phẩm đã chọn không khả dụng.";
        }

        var templateExists = await _dbContext.ProductTemplates
            .AnyAsync(template =>
                template.Id == model.ProductTemplateId.Value &&
                template.DeletedAt == null &&
                template.IsActive &&
                template.Category.DeletedAt == null &&
                template.Category.IsActive);

        if (!templateExists)
        {
            return "Mẫu sản phẩm đã chọn không khả dụng.";
        }

        var sellerExists = await _dbContext.Users
            .AnyAsync(user =>
                user.Id == model.SellerId &&
                user.DeletedAt == null &&
                user.Status == UserStatus.Active &&
                user.Role == UserRole.User);

        if (!sellerExists)
        {
            return "Người bán đã chọn không khả dụng.";
        }

        return null;
    }

    private const int MaxDocumentsPerProduct = 5;

    private static string? ValidateDocumentFiles(IEnumerable<IFormFile> files)
    {
        const long maxFileSize = 5 * 1024 * 1024;
        var uploadCount = files.Count(file => file is { Length: > 0 });
        if (uploadCount > MaxDocumentsPerProduct)
        {
            return $"Chỉ được tải tối đa {MaxDocumentsPerProduct} tài liệu cho mỗi sản phẩm.";
        }

        foreach (var file in files.Where(file => file is { Length: > 0 }))
        {
            if (file.Length > maxFileSize)
            {
                return "Dung lượng tài liệu không được vượt quá 5MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return "Tài liệu phải là file PDF.";
            }
        }

        return null;
    }

    private async Task PopulateFormOptionsAsync(
        ProductFormViewModel model,
        int? selectedCategoryId = null,
        int? selectedSellerId = null)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(selectedCategoryId ?? model.CategoryId);
        model.ProductTemplateOptions = await BuildProductTemplateOptionsAsync(model.ProductTemplateId);
        model.SellerOptions = await BuildSellerOptionsAsync(selectedSellerId ?? model.SellerId);
        model.ConditionOptions = BuildConditionOptions(model.Condition);
        model.GradeOptions = BuildGradeOptions(model.GradeLabel);
        model.LanguageOptions = BuildLanguageOptions(model.Language);
    }

    private async Task PopulateTemplateFormOptionsAsync(ProductTemplateFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.GradeOptions = BuildGradeOptions(model.GradeLabel);
        model.LanguageOptions = BuildLanguageOptions(model.Language);
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

    private async Task<List<SelectListItem>> BuildProductTemplateOptionsAsync(int? selectedId = null)
    {
        var templates = await _dbContext.ProductTemplates
            .AsNoTracking()
            .Where(template => template.DeletedAt == null && template.IsActive)
            .OrderBy(template => template.Name)
            .Select(template => new
            {
                template.Id,
                template.Name,
                CategoryName = template.Category.Name,
                template.GradeLabel
            })
            .ToListAsync();

        return templates
            .Select(template => new SelectListItem
            {
                Value = template.Id.ToString(),
                Text = $"{template.Name} ({template.CategoryName}{(string.IsNullOrWhiteSpace(template.GradeLabel) ? string.Empty : $" - {template.GradeLabel}")})",
                Selected = selectedId == template.Id
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> BuildSellerOptionsAsync(int? selectedId = null, int? templateId = null)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.DeletedAt == null &&
                user.Status == UserStatus.Active &&
                user.Role == UserRole.User);

        if (templateId.HasValue)
        {
            query = query.Where(user => user.Products.Any(product =>
                product.DeletedAt == null &&
                product.ProductTemplateId == templateId.Value));
        }

        var sellers = await query
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

    private static List<SelectListItem> BuildConditionOptions(string? selected = null)
    {
        return new[] { "Graded", "Ungraded" }
            .Select(condition => new SelectListItem
            {
                Value = condition,
                Text = FormatConditionLabel(condition),
                Selected = condition == selected
            })
            .ToList();
    }

    private static string FormatConditionLabel(string condition) =>
        condition switch
        {
            "Graded" => "Đã chấm điểm",
            "Ungraded" => "Chưa chấm điểm",
            _ => condition
        };

    private static List<SelectListItem> BuildGradeOptions(string? selected = null)
    {
        return GradeLabelHelper.GetAllGradeLabels()
            .Select(grade => new SelectListItem
            {
                Value = grade,
                Text = grade,
                Selected = grade == selected
            })
            .ToList();
    }

    private static List<SelectListItem> BuildLanguageOptions(string? selected = null)
    {
        return CreateAuctionMockData.Languages
            .Select(language => new SelectListItem
            {
                Value = language,
                Text = language,
                Selected = language == selected
            })
            .ToList();
    }

    private async Task<ProductTemplateDetailViewModel?> GetTemplateDetailsAsync(int templateId)
    {
        return await _dbContext.ProductTemplates
            .AsNoTracking()
            .Where(template => template.Id == templateId && template.DeletedAt == null && template.IsActive)
            .Select(template => new ProductTemplateDetailViewModel
            {
                Id = template.Id,
                Name = template.Name,
                CategoryName = template.Category.Name,
                SetName = template.SetName,
                CardNumber = template.CardNumber,
                GradeLabel = template.GradeLabel,
                Language = template.Language,
                Year = template.Year,
                PrimaryImage = template.PrimaryImage,
                InstanceCount = template.Products.Count(product => product.DeletedAt == null),
                SellerCount = template.Products
                    .Where(product => product.DeletedAt == null)
                    .Select(product => product.SellerId)
                    .Distinct()
                    .Count()
            })
            .FirstOrDefaultAsync();
    }

    private async Task<string?> ValidateTemplateAsync(ProductTemplateFormViewModel model)
    {
        var categoryExists = await _dbContext.Categories
            .AnyAsync(category =>
                category.Id == model.CategoryId &&
                category.DeletedAt == null &&
                category.IsActive);

        if (!categoryExists)
        {
            return "Danh mục đã chọn không khả dụng.";
        }

        if (model.PrimaryImageFile is { Length: > 0 })
        {
            const long maxImageSize = 2 * 1024 * 1024;
            var extension = Path.GetExtension(model.PrimaryImageFile.FileName).ToLowerInvariant();

            if (model.PrimaryImageFile.Length > maxImageSize)
            {
                return "Ảnh chính của mẫu sản phẩm không được vượt quá 2MB.";
            }

            if (extension is not ".jpg" and not ".jpeg" and not ".png")
            {
                return "Ảnh chính của mẫu sản phẩm phải là JPEG hoặc PNG.";
            }
        }

        if (!model.Id.HasValue && model.PrimaryImageFile is not { Length: > 0 })
        {
            return "Vui lòng tải ảnh chính cho mẫu sản phẩm.";
        }

        var normalizedName = NormalizeTemplateKey(model.Name);
        var normalizedSet = NormalizeTemplateKey(model.SetName);
        var normalizedCard = NormalizeTemplateKey(model.CardNumber);
        var normalizedGrade = NormalizeTemplateKey(model.GradeLabel);

        var candidates = await _dbContext.ProductTemplates
            .AsNoTracking()
            .Where(template =>
                template.DeletedAt == null &&
                template.IsActive &&
                template.CategoryId == model.CategoryId &&
                (!model.Id.HasValue || template.Id != model.Id.Value))
            .Select(template => new
            {
                template.Name,
                template.SetName,
                template.CardNumber,
                template.GradeLabel
            })
            .ToListAsync();

        var isDuplicate = candidates.Any(template =>
            NormalizeTemplateKey(template.Name) == normalizedName &&
            NormalizeTemplateKey(template.SetName) == normalizedSet &&
            NormalizeTemplateKey(template.CardNumber) == normalizedCard &&
            NormalizeTemplateKey(template.GradeLabel) == normalizedGrade);

        return isDuplicate
            ? "Đã tồn tại mẫu sản phẩm đang hoạt động với thông tin trùng khớp."
            : null;
    }

    private static void ApplyTemplateSnapshot(ProductFormViewModel model, ProductTemplate template)
    {
        model.ProductTemplateId = template.Id;
        model.CategoryId = template.CategoryId;
        model.Name = template.Name;
        model.ShortDescription = template.ShortDescription;
        model.DescriptionHtml = template.DescriptionHtml;
        model.SetName = template.SetName;
        model.CardNumber = template.CardNumber;
        model.GradeLabel = template.GradeLabel;
        model.Year = template.Year;
        model.Language = template.Language;
        model.PrimaryImageUrl = template.PrimaryImage;
        model.ProductTemplateName = template.Name;
    }

    public static string NormalizeTemplateKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static void NormalizeTemplateFilter(ProductTemplateFilterViewModel filter)
    {
        if (filter.Page <= 0)
        {
            filter.Page = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        if (filter.PageSize > 50)
        {
            filter.PageSize = 50;
        }
    }

    private static void ApplyProductCodes(IEnumerable<ProductListItemViewModel> products)
    {
        foreach (var product in products)
        {
            product.ProductCode = ProductDisplayHelper.FormatProductCode(product.Id);
        }
    }

    private static bool TryParseProductCode(string keyword, out int productId)
    {
        productId = 0;

        if (keyword.StartsWith("PRD-", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(keyword[4..], out productId) && productId > 0;
        }

        return int.TryParse(keyword, out productId) && productId > 0;
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

        if (filter.PageSize > 50)
        {
            filter.PageSize = 50;
        }
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ResolveDocumentType(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "PDF",
            _ => "FILE"
        };
    }

    private static string FormatStatusLabel(string status) =>
        status switch
        {
            AuctionStatuses.Live => "đang diễn ra",
            AuctionStatuses.EndingSoon => "sắp kết thúc",
            AuctionStatuses.AwaitingPayment => "đang chờ thanh toán",
            _ => status.Replace('_', ' ')
        };

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
}
