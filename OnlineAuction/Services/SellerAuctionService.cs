using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SellerAuctionService : ISellerAuctionService
{
    private const string ProductImageFolder = "auction-house/products";

    private const string DocumentFolder = "auction-house/documents";

    // Anh fallback giup products.primary_image khong bi null khi view hien tai chua gui file len server.
    // Khi view upload anh co name="PrimaryImageFile", Cloudinary URL se thay the gia tri nay.
    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private readonly AuctionHouseDbContext _db;
    private readonly IPhotoService _photoService;
    private readonly ISellService _sellService;

    public SellerAuctionService(
        AuctionHouseDbContext db,
        IPhotoService photoService,
        ISellService sellService)
    {
        _db = db;
        _photoService = photoService;
        _sellService = sellService;
    }

    public async Task<List<AuctionItemViewModel>> GetSellerAuctionsAsync(
        int sellerId,
        string? channel = null,
        bool forPublicProfile = false,
        bool includeOwnerDrafts = false,
        string? tab = null)
    {
        var normalizedChannel = channel?.ToLowerInvariant();

        var query = _db.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.Product.SellerId == sellerId &&
                auction.Status != AuctionStatuses.Cancelled &&
                (normalizedChannel == null ||
                 (normalizedChannel == ListingTypes.BuyNow
                     ? auction.ListingType == ListingTypes.BuyNow
                     : auction.ListingType == ListingTypes.Auction)));

        if (forPublicProfile)
        {
            var visibleStatuses = includeOwnerDrafts
                ? ProfileOwnerVisibleStatuses
                : ProfilePublicVisibleStatuses;

            query = query.Where(auction => visibleStatuses.Contains(auction.Status));
        }
        else if (!string.IsNullOrWhiteSpace(tab))
        {
            query = ApplySellerTabFilter(query, tab);
        }

        var auctions = await query
            .Include(auction => auction.Product)
                .ThenInclude(product => product.Category)
            .Include(auction => auction.Bids)
            .OrderByDescending(auction => auction.CreatedAt)
            .ToListAsync();

        return auctions
            .Select(auction =>
            {
                var item = ProductDetailMapper.MapToAuctionItem(
                    auction,
                    forBuyNowCatalog: auction.ListingType == ListingTypes.BuyNow);
                item.RejectReason = auction.RejectReason;
                return item;
            })
            .ToList();
    }

    private static readonly string[] ProfilePublicVisibleStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private static readonly string[] ProfileOwnerVisibleStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Confirming,
        AuctionStatuses.LegacyPendingReview,
        AuctionStatuses.Rejected,
        AuctionStatuses.Scheduled
    ];

    private static IQueryable<Auction> ApplySellerTabFilter(IQueryable<Auction> query, string tab)
    {
        var now = DateTime.UtcNow;

        return tab.ToLowerInvariant() switch
        {
            // Sold: completed sales, winner awaiting payment, or ended with a winner.
            "sold" => query.Where(a =>
                a.Status == AuctionStatuses.Completed ||
                a.Status == AuctionStatuses.AwaitingPayment ||
                (a.Status == AuctionStatuses.Ended &&
                 (a.WinnerId != null || a.Bids.Any(b => b.IsWinning)))),

            // Unsold: ended with nobody winning / no sale completed.
            "unsold" => query.Where(a =>
                a.Status == AuctionStatuses.Ended &&
                a.WinnerId == null &&
                !a.Bids.Any(b => b.IsWinning)),

            // Scheduled: approved but not yet at registration start (Upcoming).
            "scheduled" => query.Where(a =>
                a.Status == AuctionStatuses.Scheduled &&
                a.RegistrationStartDate > now),

            // Active: admin-approved listings in marketplace phases:
            // registration open, registration closed (awaiting live), live, ending soon.
            // Confirming stays on Account/Submissions (and owner profile).
            _ => query.Where(a =>
                a.Status == AuctionStatuses.Live ||
                a.Status == AuctionStatuses.EndingSoon ||
                (a.Status == AuctionStatuses.Scheduled &&
                 a.RegistrationStartDate <= now &&
                 a.EndDate > now))
        };
    }

    public async Task<(bool Success, string Message, int? AuctionId)> CreateAsync(
        CreateAuctionViewModel model,
        int sellerId)
    {
        if (model.EndDate <= model.StartDate)
        {
            return (false, "Live end must be greater than live start.", null);
        }

        var scheduleError = AuctionScheduleHelper.ValidateSchedule(
            model.RegistrationStartDate,
            model.RegistrationEndDate,
            model.StartDate,
            model.EndDate);

        if (scheduleError is not null)
        {
            return (false, scheduleError, null);
        }

        if (model.StartingPrice <= 0 || model.BidStep <= 0)
        {
            return (false, "Starting price and bid step must be greater than 0.", null);
        }

        if (model.Year is < 1800 or > 2100)
        {
            return (false, "Please enter a valid year between 1800 and 2100.", null);
        }

        SellService.NormalizeGradingFields(model);

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .Take(4)
            .ToList();

        if (1 + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images.", null);
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation, null);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateAuctionCoreAsync(model, sellerId, galleryFiles));
    }

    private async Task<(bool Success, string Message, int? AuctionId)> CreateAuctionCoreAsync(
        CreateAuctionViewModel model,
        int sellerId,
        IReadOnlyList<IFormFile> galleryFiles)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
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
                ShortDescription = TrimOrNull(model.ShortDescription),
                Subtitle = TrimOrNull(model.Subtitle),
                DescriptionHtml = model.ProductDescription,
                Condition = model.Condition,
                Year = model.Year,
                SetName = TrimOrNull(model.SetName),
                Language = TrimOrNull(model.Language),
                CardNumber = TrimOrNull(model.CardNumber),
                GradeLabel = TrimOrNull(model.Grade),
                CertNumber = TrimOrNull(model.CertificateNumber),
                PrimaryImage = imageUrl,
                Category = category,
                CreatedAt = now
            };

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
                    return (false, ex.Message, null);
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
                    return (false, ex.Message, null);
                }
            }

            var auction = new Auction
            {
                Product = product,
                StartingPrice = model.StartingPrice,
                BidStep = model.BidStep,
                CurrentPrice = model.StartingPrice,
                BuyNowPrice = model.BuyNowPrice,
                RegistrationStartDate = model.RegistrationStartDate,
                RegistrationEndDate = model.RegistrationEndDate,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ListingType = ListingTypes.Auction,
                Status = AuctionStatuses.Confirming,
                SubmittedAt = now,
                CreatedAt = now
            };

            _db.Auctions.Add(auction);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Your listing is confirming / awaiting admin confirmation.", auction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Success, string Message, int? AuctionId)> CreateBuyNowAsync(
        CreateBuyNowViewModel model,
        int sellerId)
    {
        if (model.Price <= 0)
        {
            return (false, "Price must be greater than 0.", null);
        }

        if (model.Year is < 1800 or > 2100)
        {
            return (false, "Please enter a valid year between 1800 and 2100.", null);
        }

        SellService.NormalizeGradingFields(model);

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .Take(4)
            .ToList();

        if (1 + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images.", null);
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation, null);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateBuyNowCoreAsync(model, sellerId, galleryFiles));
    }

    private async Task<(bool Success, string Message, int? AuctionId)> CreateBuyNowCoreAsync(
        CreateBuyNowViewModel model,
        int sellerId,
        IReadOnlyList<IFormFile> galleryFiles)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
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
                ShortDescription = TrimOrNull(model.ShortDescription),
                Subtitle = TrimOrNull(model.Subtitle),
                DescriptionHtml = model.ProductDescription,
                Condition = model.Condition,
                Year = model.Year,
                SetName = TrimOrNull(model.SetName),
                Language = TrimOrNull(model.Language),
                CardNumber = TrimOrNull(model.CardNumber),
                GradeLabel = TrimOrNull(model.Grade),
                CertNumber = TrimOrNull(model.CertificateNumber),
                PrimaryImage = imageUrl,
                Category = category,
                CreatedAt = now
            };

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
                    return (false, ex.Message, null);
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
                    return (false, ex.Message, null);
                }
            }

            var buyNowScheduleStart = now;
            var buyNowLiveStart = buyNowScheduleStart.AddMinutes(1);
            var buyNowPrice = model.Price;
            var startingPrice = ResolveBuyNowStartingPrice(buyNowPrice);

            var auction = new Auction
            {
                Product = product,
                StartingPrice = startingPrice,
                BidStep = 0.01m,
                CurrentPrice = buyNowPrice,
                BuyNowPrice = buyNowPrice,
                RequiresRegistration = false,
                RegistrationStartDate = buyNowScheduleStart,
                RegistrationEndDate = buyNowLiveStart,
                StartDate = buyNowLiveStart,
                EndDate = now.AddYears(1),
                ListingType = ListingTypes.BuyNow,
                Status = AuctionStatuses.Confirming,
                SubmittedAt = now,
                CreatedAt = now
            };

            _db.Auctions.Add(auction);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Your listing is confirming / awaiting admin confirmation.", auction.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SellerAuctionFormViewModel?> GetEditFormAsync(int auctionId, int sellerId)
    {
        var auction = await _db.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .ThenInclude(product => product.Category)
            .Include(a => a.Product)
            .ThenInclude(product => product.Images)
            .Include(a => a.Product)
            .ThenInclude(product => product.Documents)
            .FirstOrDefaultAsync(a => a.Id == auctionId && a.Product.SellerId == sellerId);

        if (auction is null)
        {
            return null;
        }

        if (!IsEditableStatus(auction))
        {
            return null;
        }

        var hasBids = await _db.Bids.AnyAsync(b => b.AuctionId == auction.Id);
        GradeLabelHelper.Parse(auction.Product.GradeLabel, out var authenticator, out var gradeValue);

        var model = new SellerAuctionFormViewModel
        {
            AuctionId = auction.Id,
            Status = auction.Status,
            HasBids = hasBids,
            ProductName = auction.Product.Name,
            Category = auction.Product.Category.Name,
            ShortDescription = auction.Product.ShortDescription,
            Subtitle = auction.Product.Subtitle,
            ProductDescription = auction.Product.DescriptionHtml,
            Condition = auction.Product.Condition,
            Year = auction.Product.Year,
            SetName = auction.Product.SetName ?? string.Empty,
            Language = auction.Product.Language ?? "English",
            CardNumber = auction.Product.CardNumber,
            Authenticator = authenticator,
            GradeValue = gradeValue,
            Grade = auction.Product.GradeLabel ?? GradeLabelHelper.Compose(authenticator, gradeValue),
            CertificateNumber = auction.Product.CertNumber,
            ExistingPrimaryImage = auction.Product.PrimaryImage,
            StartingPrice = string.Equals(auction.ListingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase)
                ? ResolveBuyNowStartingPrice(auction.BuyNowPrice ?? auction.CurrentPrice)
                : auction.StartingPrice,
            BidStep = string.Equals(auction.ListingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase)
                ? 0.01m
                : auction.BidStep,
            BuyNowPrice = string.Equals(auction.ListingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase)
                ? auction.BuyNowPrice ?? auction.CurrentPrice
                : auction.BuyNowPrice,
            StartDate = DateTimeUtilities.AsUtc(auction.StartDate),
            EndDate = DateTimeUtilities.AsUtc(auction.EndDate),
            RegistrationStartDate = DateTimeUtilities.AsUtc(auction.RegistrationStartDate),
            RegistrationEndDate = DateTimeUtilities.AsUtc(auction.RegistrationEndDate),
            ExistingGalleryImages = auction.Product.Images
                .Where(image => image.DeletedAt == null)
                .OrderBy(image => image.SortOrder)
                .Select(image => new SellerAuctionExistingImageViewModel
                {
                    Id = image.Id,
                    Url = image.ImageUrl,
                    SortOrder = image.SortOrder
                })
                .ToList(),
            ExistingDocuments = auction.Product.Documents
                .Where(document => document.DeletedAt == null)
                .OrderBy(document => document.Id)
                .Select(document => new SellerAuctionExistingDocumentViewModel
                {
                    Id = document.Id,
                    Name = document.Name,
                    FileUrl = document.FileUrl
                })
                .ToList()
        };

        ApplyEditLocks(model, auction, hasBids);
        _sellService.PopulateOptions(model);
        SellService.NormalizeGradingFields(model);
        return model;
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
            .ThenInclude(product => product.Images)
            .Include(a => a.Product)
            .ThenInclude(product => product.Documents)
            .FirstOrDefaultAsync(a => a.Id == model.AuctionId && a.Product.SellerId == sellerId);

        if (auction is null)
        {
            return (false, "Auction not found.");
        }

        if (!IsEditableStatus(auction))
        {
            return (false, "Only pending, scheduled, or live listings can be edited.");
        }

        var hasBids = await _db.Bids.AnyAsync(b => b.AuctionId == auction.Id);
        ApplyEditLocks(model, auction, hasBids);

        if (model.LockRegistrationDates)
        {
            model.RegistrationStartDate = auction.RegistrationStartDate;
            model.RegistrationEndDate = auction.RegistrationEndDate;
        }

        if (model.LockLiveStartDate)
        {
            model.StartDate = auction.StartDate;
        }

        if (model.LockStartingPrice)
        {
            model.StartingPrice = auction.StartingPrice;
        }

        if (model.LockBidStep)
        {
            model.BidStep = auction.BidStep;
        }

        if (DateTimeUtilities.AsUtc(model.EndDate) < DateTimeUtilities.AsUtc(auction.EndDate))
        {
            return (false, "End date can only be extended, not shortened.");
        }

        SellService.NormalizeGradingFields(model);

        foreach (var (key, message) in _sellService.ValidateCreateAuction(model))
        {
            // Past registration start is allowed on edit when dates are locked / already open.
            if (model.LockRegistrationDates
                && key == nameof(model.RegistrationStartDate))
            {
                continue;
            }

            if (key == nameof(model.RegistrationStartDate)
                && DateTimeUtilities.AsUtc(model.RegistrationStartDate)
                    == DateTimeUtilities.AsUtc(auction.RegistrationStartDate))
            {
                continue;
            }

            return (false, message);
        }

        if (model.EndDate <= model.StartDate)
        {
            return (false, "Live end must be greater than live start.");
        }

        if (!model.LockRegistrationDates)
        {
            var scheduleError = AuctionScheduleHelper.ValidateSchedule(
                model.RegistrationStartDate,
                model.RegistrationEndDate,
                model.StartDate,
                model.EndDate);

            if (scheduleError is not null)
            {
                return (false, scheduleError);
            }
        }

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .Take(4)
            .ToList();

        var remainingGalleryCount = auction.Product.Images.Count(image =>
            image.DeletedAt == null && !model.RemovedGalleryImageIds.Contains(image.Id));
        if (1 + remainingGalleryCount + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images.");
        }

        var remainingDocCount = auction.Product.Documents.Count(document =>
            document.DeletedAt == null && !model.RemovedDocumentIds.Contains(document.Id));
        var newDocCount = model.DocumentFiles.Count(file => file is { Length: > 0 });
        if (remainingDocCount + newDocCount > MaxDocumentsPerProduct)
        {
            return (false, $"You can upload up to {MaxDocumentsPerProduct} documents per product.");
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => UpdateAuctionCoreAsync(model, auction, hasBids, galleryFiles));
    }

    private async Task<(bool Success, string Message)> UpdateAuctionCoreAsync(
        SellerAuctionFormViewModel model,
        Auction auction,
        bool hasBids,
        IReadOnlyList<IFormFile> galleryFiles)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            string? newImageUrl;
            try
            {
                newImageUrl = await _photoService.AddPhotoAsync(model.PrimaryImageFile, ProductImageFolder);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            var category = await GetOrCreateCategoryAsync(model.Category);
            var now = DateTime.UtcNow;

            auction.Product.Name = model.ProductName.Trim();
            auction.Product.CategoryId = category.Id;
            auction.Product.Category = category;
            auction.Product.ShortDescription = TrimOrNull(model.ShortDescription);
            auction.Product.Subtitle = TrimOrNull(model.Subtitle);
            auction.Product.DescriptionHtml = model.ProductDescription;
            auction.Product.Condition = model.Condition;
            auction.Product.Year = model.Year;
            auction.Product.SetName = TrimOrNull(model.SetName);
            auction.Product.Language = TrimOrNull(model.Language);
            auction.Product.CardNumber = TrimOrNull(model.CardNumber);
            auction.Product.GradeLabel = TrimOrNull(model.Grade);
            auction.Product.CertNumber = TrimOrNull(model.CertificateNumber);
            auction.Product.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(newImageUrl))
            {
                auction.Product.PrimaryImage = newImageUrl;
            }
            else if (!string.IsNullOrWhiteSpace(model.ExistingPrimaryImage))
            {
                auction.Product.PrimaryImage = model.ExistingPrimaryImage;
            }

            foreach (var image in auction.Product.Images.Where(image =>
                         image.DeletedAt == null && model.RemovedGalleryImageIds.Contains(image.Id)))
            {
                image.DeletedAt = now;
                image.UpdatedAt = now;
            }

            if (string.IsNullOrWhiteSpace(auction.Product.PrimaryImage))
            {
                var fallbackGallery = auction.Product.Images
                    .Where(image => image.DeletedAt == null)
                    .OrderBy(image => image.SortOrder)
                    .FirstOrDefault();
                if (fallbackGallery is not null)
                {
                    auction.Product.PrimaryImage = fallbackGallery.ImageUrl;
                    fallbackGallery.DeletedAt = now;
                    fallbackGallery.UpdatedAt = now;
                }
                else
                {
                    auction.Product.PrimaryImage = DefaultProductImageUrl;
                }
            }

            foreach (var document in auction.Product.Documents.Where(document =>
                         document.DeletedAt == null && model.RemovedDocumentIds.Contains(document.Id)))
            {
                document.DeletedAt = now;
                document.UpdatedAt = now;
            }

            var sortOrder = auction.Product.Images
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

                    auction.Product.Images.Add(new ProductImage
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

                    auction.Product.Documents.Add(new ProductDocument
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

            if (!model.LockStartingPrice)
            {
                auction.StartingPrice = model.StartingPrice;
                if (!hasBids)
                {
                    auction.CurrentPrice = model.StartingPrice;
                }
            }

            if (!model.LockBidStep)
            {
                auction.BidStep = model.BidStep;
            }

            if (string.Equals(auction.ListingType, ListingTypes.BuyNow, StringComparison.OrdinalIgnoreCase))
            {
                // CHECK chk_auctions_prices requires buy_now_price > starting_price.
                var buyNowPrice = model.BuyNowPrice ?? model.StartingPrice;
                if (buyNowPrice <= 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "Buy now price must be greater than 0.");
                }

                auction.BuyNowPrice = buyNowPrice;
                auction.StartingPrice = ResolveBuyNowStartingPrice(buyNowPrice);
                auction.BidStep = 0.01m;
                if (!hasBids)
                {
                    auction.CurrentPrice = buyNowPrice;
                }
            }
            else if (model.BuyNowPrice is not null && model.BuyNowPrice <= auction.StartingPrice)
            {
                await transaction.RollbackAsync();
                return (false, "Buy now price must be greater than the starting price.");
            }
            else
            {
                auction.BuyNowPrice = model.BuyNowPrice;
            }

            if (!model.LockRegistrationDates)
            {
                auction.RegistrationStartDate = model.RegistrationStartDate;
                auction.RegistrationEndDate = model.RegistrationEndDate;
            }

            if (!model.LockLiveStartDate)
            {
                auction.StartDate = model.StartDate;
            }

            auction.EndDate = model.EndDate;
            auction.UpdatedAt = now;

            if (auction.Status == AuctionStatuses.Rejected)
            {
                auction.Status = AuctionStatuses.Confirming;
                auction.SubmittedAt = now;
                auction.RejectReason = null;
                auction.VerifiedAt = null;
                auction.VerifiedBy = null;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = AuctionStatuses.IsConfirming(auction.Status)
                ? "Listing updated and resubmitted for admin confirmation."
                : "Auction updated successfully.";

            return (true, message);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static bool IsEditableStatus(Auction auction)
    {
        if (auction.Status is AuctionStatuses.Confirming
            or AuctionStatuses.LegacyPendingReview
            or AuctionStatuses.Rejected
            or AuctionStatuses.Scheduled)
        {
            return true;
        }

        return auction.Status is AuctionStatuses.Live or AuctionStatuses.EndingSoon
               && DateTimeUtilities.IsInFutureUtc(auction.EndDate);
    }

    private static void ApplyEditLocks(SellerAuctionFormViewModel model, Auction auction, bool hasBids)
    {
        var now = DateTime.UtcNow;
        var registrationOpened = now >= DateTimeUtilities.AsUtc(auction.RegistrationStartDate);
        var liveStarted = now >= DateTimeUtilities.AsUtc(auction.StartDate);

        var canEditFull = AuctionStatuses.IsConfirming(auction.Status)
            || auction.Status == AuctionStatuses.Rejected
            || (auction.Status == AuctionStatuses.Scheduled && !registrationOpened);

        model.CanEditFull = canEditFull;
        model.LockRegistrationDates = !canEditFull;
        model.LockLiveStartDate = !canEditFull || liveStarted;
        model.LockStartingPrice = hasBids || (!canEditFull && (registrationOpened || liveStarted));
        model.LockBidStep = hasBids;
        model.HasBids = hasBids;
        model.Status = auction.Status;
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

        if (auction.Status is not (AuctionStatuses.Live or AuctionStatuses.EndingSoon or AuctionStatuses.Confirming or AuctionStatuses.LegacyPendingReview or AuctionStatuses.Rejected or AuctionStatuses.Scheduled)
            || ((auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon)
                && !DateTimeUtilities.IsInFutureUtc(auction.EndDate)))
        {
            return (false, "This listing cannot be cancelled.");
        }

        // Soft delete: khong xoa row khoi database, chi doi status de con lich su.
        auction.Status = AuctionStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return (true, "Auction cancelled successfully.");
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

    private const int MaxDocumentsPerProduct = 5;

    /// <summary>
    /// Buy Now listings store the visible price in buy_now_price/current_price.
    /// starting_price must stay strictly lower to satisfy chk_auctions_prices.
    /// </summary>
    private static decimal ResolveBuyNowStartingPrice(decimal price) =>
        price <= 0.01m ? 0.01m : price - 0.01m;

    private static string? ValidateDocumentFiles(IEnumerable<IFormFile> files)
    {
        const long maxFileSize = 5 * 1024 * 1024;
        var uploadCount = files.Count(file => file is { Length: > 0 });
        if (uploadCount > MaxDocumentsPerProduct)
        {
            return $"You can upload up to {MaxDocumentsPerProduct} documents per product.";
        }

        foreach (var file in files.Where(file => file is { Length: > 0 }))
        {
            if (file.Length > maxFileSize)
            {
                return "Document file size must not exceed 5MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return "Documents must be PDF files.";
            }
        }

        return null;
    }
}
