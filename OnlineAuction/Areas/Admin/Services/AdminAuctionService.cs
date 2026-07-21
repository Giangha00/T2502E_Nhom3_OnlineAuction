using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Areas.Admin.ViewModels.Auctions;
using OnlineAuction.Areas.Admin.ViewModels.Products;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Areas.Admin.Services;

public class AdminAuctionService
{
    public const int BidHistoryPageSize = 20;

    private const string ProductImageFolder = "auction-house/products";
    private const string DocumentFolder = "auction-house/documents";
    private const int MaxGalleryImages = 4;
    private const int MaxDocumentsPerProduct = 5;

    private const string DefaultProductImageUrl =
        "https://res.cloudinary.com/demo/image/upload/c_fill,w_900,h_900,q_auto,f_auto/sample.jpg";

    private static readonly string[] AllowedStatuses =
    [
        AuctionStatuses.Confirming,
        AuctionStatuses.Rejected,
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Ended,
        AuctionStatuses.AwaitingPayment,
        AuctionStatuses.Completed,
        AuctionStatuses.Cancelled
    ];

    private static readonly string[] AllowedListingPhases =
    [
        AuctionListingPhases.Upcoming,
        AuctionListingPhases.RegistrationOpen,
        AuctionListingPhases.RegistrationClosed,
        AuctionListingPhases.LiveAuction,
        AuctionListingPhases.LiveEndingSoon,
        AuctionListingPhases.Ended,
        AuctionListingPhases.NotListed
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IPhotoService _photoService;
    private readonly ILogger<AdminAuctionService> _logger;

    public AdminAuctionService(
        AuctionHouseDbContext dbContext,
        IPhotoService photoService,
        ILogger<AdminAuctionService> logger)
    {
        _dbContext = dbContext;
        _photoService = photoService;
        _logger = logger;
    }

    public async Task<AuctionListViewModel> GetAuctionsAsync(AuctionFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.DeletedAt == null &&
                auction.Product.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.ListingType))
        {
            query = query.Where(auction => auction.ListingType == filter.ListingType);
        }

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
            if (AuctionStatuses.IsConfirming(filter.Status))
            {
                query = query.Where(auction => AuctionStatuses.ConfirmingStatuses.Contains(auction.Status));
            }
            else
            {
                query = query.Where(auction => auction.Status == filter.Status);
            }
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

        var auctions = await query
            .Select(auction => new AuctionListItemViewModel
            {
                Id = auction.Id,
                ProductName = auction.Product.Name,
                CategoryName = auction.Product.Category.Name,
                SellerName = auction.Product.Seller.FullName,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                BidStep = auction.BidStep,
                Status = auction.Status,
                ListingType = auction.ListingType,
                RegistrationStartDate = auction.RegistrationStartDate,
                RegistrationEndDate = auction.RegistrationEndDate,
                StartDate = auction.StartDate,
                EndDate = auction.EndDate,
                BidCount = auction.Bids.Count(bid => bid.DeletedAt == null),
                RegistrationCount = auction.Registrations.Count(registration => registration.DeletedAt == null),
                ImageUrl = auction.Product.PrimaryImage,
                VerifiedAt = auction.VerifiedAt,
                CreatedAt = auction.CreatedAt
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var auction in auctions)
        {
            ApplyListingDisplayInfo(auction, now);
        }

        if (!string.IsNullOrWhiteSpace(filter.ListingPhase))
        {
            auctions = auctions
                .Where(auction => auction.ListingPhase == filter.ListingPhase)
                .ToList();
        }

        var totalItems = auctions.Count;
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        auctions = auctions
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new AuctionListViewModel
        {
            Auctions = auctions,
            Filter = filter,
            CategoryOptions = await BuildCategoryOptionsAsync(filter.CategoryId),
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<AuctionDetailViewModel?> GetDetailsAsync(
        int id,
        int bidPage = 1,
        bool flaggedOnly = false)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Where(item => item.Id == id && item.DeletedAt == null && item.Product.DeletedAt == null)
            .Select(item => new AuctionDetailViewModel
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Description = item.Product.DescriptionHtml ?? item.Product.ShortDescription ?? string.Empty,
                CategoryName = item.Product.Category.Name,
                SellerName = item.Product.Seller.FullName,
                SellerEmail = item.Product.Seller.Email ?? string.Empty,
                StartingPrice = item.StartingPrice,
                BidStep = item.BidStep,
                CurrentPrice = item.CurrentPrice,
                BuyNowPrice = item.BuyNowPrice,
                Status = item.Status,
                ListingType = item.ListingType,
                RequiresRegistration = item.RequiresRegistration,
                RegistrationStartDate = item.RegistrationStartDate,
                RegistrationEndDate = item.RegistrationEndDate,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                ImageUrl = item.Product.PrimaryImage,
                BidCount = item.Bids.Count(bid => bid.DeletedAt == null),
                RegistrationCount = item.Registrations.Count(registration => registration.DeletedAt == null),
                WinnerName = item.Winner != null ? item.Winner.FullName : null,
                OrderId = item.OrderItems
                    .Where(orderItem => orderItem.DeletedAt == null)
                    .Select(orderItem => (int?)orderItem.OrderId)
                    .FirstOrDefault(),
                OrderReference = item.OrderItems
                    .Where(orderItem => orderItem.DeletedAt == null)
                    .Select(orderItem => orderItem.Order.OrderReference)
                    .FirstOrDefault(),
                OrderStatus = item.OrderItems
                    .Where(orderItem => orderItem.DeletedAt == null)
                    .Select(orderItem => orderItem.Order.Status)
                    .FirstOrDefault(),
                PaymentDeadline = item.OrderItems
                    .Where(orderItem => orderItem.DeletedAt == null)
                    .Select(orderItem => (DateTime?)orderItem.Order.PaymentDeadline)
                    .FirstOrDefault(),
                SubmittedAt = item.SubmittedAt,
                VerifiedAt = item.VerifiedAt,
                VerifiedByName = item.Verifier != null ? item.Verifier.FullName : null,
                RejectReason = item.RejectReason,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (auction is null)
        {
            return null;
        }

        auction.ShowFlaggedBidsOnly = flaggedOnly;
        auction.FraudAlerts = await LoadFraudAlertsAsync(id);
        ApplyListingDisplayInfo(auction, DateTime.UtcNow);

        var bidQuery = _dbContext.Bids
            .AsNoTracking()
            .Where(bid => bid.AuctionId == id && bid.DeletedAt == null);

        if (flaggedOnly)
        {
            bidQuery = bidQuery.Where(bid => bid.IsFlagged);
        }

        var bidHistoryTotalCount = await bidQuery.CountAsync();

        auction.BidHistoryTotalCount = bidHistoryTotalCount;
        auction.BidCount = bidHistoryTotalCount;
        auction.BidHistoryPageSize = BidHistoryPageSize;

        if (bidPage <= 0)
        {
            bidPage = 1;
        }

        auction.BidHistoryTotalPages = bidHistoryTotalCount == 0
            ? 1
            : (int)Math.Ceiling(bidHistoryTotalCount / (double)BidHistoryPageSize);

        if (bidPage > auction.BidHistoryTotalPages)
        {
            bidPage = auction.BidHistoryTotalPages;
        }

        auction.BidHistoryPage = bidPage;

        auction.WinnerNonPaymentLogs = await LoadWinnerNonPaymentLogsAsync(id);
        auction.ForfeitedDeposits = await LoadForfeitedDepositsAsync(id);

        if (bidHistoryTotalCount == 0)
        {
            auction.BidHistory = [];
            return auction;
        }

        var skip = (bidPage - 1) * BidHistoryPageSize;
        var bids = await bidQuery
            .Include(bid => bid.Bidder)
            .OrderByDescending(bid => bid.PlacedAt)
            .Skip(skip)
            .Take(BidHistoryPageSize)
            .ToListAsync();

        auction.BidHistory = AdminBidHistoryMapper.Map(bids, skip);

        return auction;
    }

    public async Task<AuctionFormViewModel> BuildCreateAuctionFormAsync()
    {
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new AuctionFormViewModel
        {
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            BidStep = 50,
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.Auction,
            Authenticator = "PSA",
            GradeValue = "10",
            Language = "English"
        };

        await PopulateFormOptionsAsync(model);
        model.NormalizeGrading();
        return model;
    }

    public async Task<AuctionFormViewModel> BuildCreateBuyNowFormAsync()
    {
        var model = new AuctionFormViewModel
        {
            Status = AuctionStatuses.Confirming,
            ListingType = ListingTypes.BuyNow,
            Authenticator = "PSA",
            GradeValue = "10",
            Language = "English"
        };

        await PopulateFormOptionsAsync(model);
        model.NormalizeGrading();
        return model;
    }

    public async Task<AuctionFormViewModel?> GetEditFormAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(item => item.Product)
                .ThenInclude(product => product.Images)
            .Include(item => item.Product)
                .ThenInclude(product => product.Documents)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null && item.Product.DeletedAt == null);

        if (auction is null)
        {
            return null;
        }

        GradeLabelHelper.Parse(auction.Product.GradeLabel, out var authenticator, out var gradeValue);

        var model = new AuctionFormViewModel
        {
            Id = auction.Id,
            ProductId = auction.ProductId,
            ProductName = auction.Product.Name,
            ShortDescription = auction.Product.ShortDescription,
            ProductDescription = auction.Product.DescriptionHtml,
            Subtitle = auction.Product.Subtitle,
            Condition = auction.Product.Condition ?? "Graded",
            Year = auction.Product.Year,
            SetName = auction.Product.SetName,
            Authenticator = authenticator,
            GradeValue = gradeValue,
            Grade = auction.Product.GradeLabel ?? GradeLabelHelper.Compose(authenticator, gradeValue),
            Language = auction.Product.Language ?? "English",
            CardNumber = auction.Product.CardNumber,
            CertificateNumber = auction.Product.CertNumber,
            StartingPrice = auction.StartingPrice,
            BidStep = auction.BidStep,
            BuyNowPrice = auction.BuyNowPrice,
            Price = auction.ListingType == ListingTypes.BuyNow
                ? auction.BuyNowPrice ?? auction.StartingPrice
                : 0,
            CurrentPrice = auction.CurrentPrice,
            RegistrationStartDate = auction.RegistrationStartDate,
            RegistrationEndDate = auction.RegistrationEndDate,
            StartDate = auction.StartDate,
            EndDate = auction.EndDate,
            Status = auction.Status,
            ListingType = auction.ListingType,
            CategoryId = auction.Product.CategoryId,
            SellerId = auction.Product.SellerId,
            ImageUrl = auction.Product.PrimaryImage,
            ExistingGalleryImages = auction.Product.Images
                .Where(image => image.DeletedAt == null)
                .OrderBy(image => image.SortOrder)
                .Select(image => new ProductImageItemViewModel
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    SortOrder = image.SortOrder
                })
                .ToList(),
            ExistingDocuments = auction.Product.Documents
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
            BidCount = await _dbContext.Bids.CountAsync(bid => bid.AuctionId == id && bid.DeletedAt == null)
        };

        await PopulateFormOptionsAsync(model);
        model.NormalizeGrading();
        return model;
    }

    public async Task<(bool Success, string Message)> CreateAsync(AuctionFormViewModel model)
    {
        model.NormalizeGrading();

        var validationError = await ValidateReferencesAsync(model);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        if (model.Year is < 1800 or > 2100)
        {
            return (false, "Please enter a valid year between 1800 and 2100.");
        }

        var galleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .Take(MaxGalleryImages)
            .ToList();

        if (1 + galleryFiles.Count > 5)
        {
            return (false, "You can upload up to 5 images.");
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateCoreAsync(model, galleryFiles));
    }

    public async Task<(bool Success, string Message)> UpdateAsync(AuctionFormViewModel model)
    {
        if (model.Id <= 0)
        {
            return (false, "Auction id is required.");
        }

        model.NormalizeGrading();

        var auction = await _dbContext.Auctions
            .Include(item => item.Product)
                .ThenInclude(product => product.Images)
            .Include(item => item.Product)
                .ThenInclude(product => product.Documents)
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

        if (model.Year is < 1800 or > 2100)
        {
            return (false, "Please enter a valid year between 1800 and 2100.");
        }

        if (!model.IsBuyNow && bidCount > 0 && model.StartingPrice > auction.CurrentPrice)
        {
            return (false, "Starting price cannot exceed the current price when bids exist.");
        }

        var newGalleryFiles = model.GalleryImageFiles
            .Where(file => file is { Length: > 0 })
            .ToList();

        var remainingGalleryCount = auction.Product.Images.Count(image =>
            image.DeletedAt == null && !model.RemoveGalleryImageIds.Contains(image.Id));

        if (remainingGalleryCount + newGalleryFiles.Count > MaxGalleryImages)
        {
            return (false, $"Gallery can contain at most {MaxGalleryImages} images.");
        }

        var documentValidation = ValidateDocumentFiles(model.DocumentFiles);
        if (documentValidation is not null)
        {
            return (false, documentValidation);
        }

        var remainingDocumentCount = auction.Product.Documents.Count(document =>
            document.DeletedAt == null && !model.RemoveDocumentIds.Contains(document.Id));
        var newDocumentCount = model.DocumentFiles.Count(file => file is { Length: > 0 });
        if (remainingDocumentCount + newDocumentCount > MaxDocumentsPerProduct)
        {
            return (false, $"Each product can have at most {MaxDocumentsPerProduct} documents.");
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

            if (string.IsNullOrWhiteSpace(newImageUrl))
            {
                return (false, "Image upload failed.");
            }
        }

        var now = DateTime.UtcNow;

        ApplyProductFields(auction.Product, model);
        if (!string.IsNullOrWhiteSpace(newImageUrl))
        {
            auction.Product.PrimaryImage = newImageUrl;
        }

        await ApplyGalleryChangesAsync(auction.Product, model, newGalleryFiles, now);
        await ApplyDocumentChangesAsync(auction.Product, model, now);

        auction.Product.UpdatedAt = now;

        if (model.IsBuyNow)
        {
            var startingPrice = ResolveBuyNowStartingPrice(model.Price);
            auction.StartingPrice = startingPrice;
            auction.BidStep = 0.01m;
            auction.BuyNowPrice = model.Price;
            auction.ListingType = ListingTypes.BuyNow;
            auction.RequiresRegistration = false;

            if (bidCount == 0)
            {
                auction.CurrentPrice = model.Price;
            }
        }
        else
        {
            auction.StartingPrice = model.StartingPrice;
            auction.BidStep = model.BidStep;
            auction.BuyNowPrice = model.BuyNowPrice;
            auction.ListingType = ListingTypes.Auction;
            auction.RequiresRegistration = true;
            auction.RegistrationStartDate = model.RegistrationStartDate;
            auction.RegistrationEndDate = model.RegistrationEndDate;
            auction.StartDate = model.StartDate;
            auction.EndDate = model.EndDate;

            if (bidCount == 0)
            {
                auction.CurrentPrice = model.StartingPrice;
            }
        }

        auction.Status = model.Status;
        auction.UpdatedAt = now;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Could not update listing. Check prices, dates, and selected references.");
        }

        return (true, model.IsBuyNow ? "Buy Now listing updated successfully." : "Auction updated successfully.");
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

    public async Task<(bool Success, string Message)> BulkDeleteAsync(IReadOnlyList<int> auctionIds)
    {
        if (auctionIds.Count == 0)
        {
            return (false, "Please select at least one auction.");
        }

        var auctions = await _dbContext.Auctions
            .Include(item => item.Product)
            .Include(item => item.Bids)
            .Include(item => item.OrderItems)
            .Where(item => auctionIds.Contains(item.Id) && item.DeletedAt == null)
            .ToListAsync();

        if (auctions.Count == 0)
        {
            return (false, "No auctions found.");
        }

        var deletedCount = 0;
        var skippedMessages = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var auction in auctions)
        {
            var activeBidCount = auction.Bids.Count(bid => bid.DeletedAt == null);
            if (activeBidCount > 0)
            {
                skippedMessages.Add($"#{auction.Id}: has {activeBidCount} bid(s)");
                continue;
            }

            var activeOrderCount = auction.OrderItems.Count(item => item.DeletedAt == null);
            if (activeOrderCount > 0)
            {
                skippedMessages.Add($"#{auction.Id}: linked to an order");
                continue;
            }

            auction.DeletedAt = now;
            auction.UpdatedAt = now;
            auction.Product.DeletedAt = now;
            auction.Product.UpdatedAt = now;
            deletedCount++;
        }

        if (deletedCount == 0)
        {
            return (false, string.Join(" ", skippedMessages));
        }

        await _dbContext.SaveChangesAsync();

        if (skippedMessages.Count == 0)
        {
            return (true, $"Deleted {deletedCount} auction(s) successfully.");
        }

        return (true, $"Deleted {deletedCount} auction(s). Skipped {skippedMessages.Count}: {string.Join(" ", skippedMessages)}");
    }

    public async Task PopulateFormOptionsAsync(AuctionFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync(model.CategoryId);
        model.SellerOptions = await BuildSellerOptionsAsync(model.SellerId);
        model.StatusOptions = BuildStatusOptions(model.Status);
        model.Authenticators = GradeLabelHelper.Authenticators.ToList();
        model.GradeValues = GradeLabelHelper.GradeValues.ToList();
        model.Languages = CreateAuctionMockData.Languages.ToList();
    }

    public async Task<(bool Success, string Message)> ReviewFraudAlertAsync(
        long alertId,
        int adminId,
        string status)
    {
        if (status is not (FraudAlertStatuses.Reviewed or FraudAlertStatuses.Dismissed))
        {
            return (false, "Invalid fraud alert action.");
        }

        var alert = await _dbContext.BidFraudAlerts.FirstOrDefaultAsync(item => item.Id == alertId);
        if (alert is null)
        {
            return (false, "Fraud alert not found.");
        }

        alert.Status = status;
        alert.ReviewedBy = adminId > 0 ? adminId : null;
        alert.ReviewedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Admin {AdminId} marked fraud alert {AlertId} as {Status}.",
            adminId,
            alertId,
            status);

        return (true, status == FraudAlertStatuses.Reviewed
            ? "Fraud alert marked as reviewed."
            : "Fraud alert dismissed.");
    }

    private async Task<(bool Success, string Message)> CreateCoreAsync(
        AuctionFormViewModel model,
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
                ? DefaultProductImageUrl
                : imageUrl;

            var now = DateTime.UtcNow;
            var product = new Product
            {
                SellerId = model.SellerId,
                CategoryId = model.CategoryId,
                PrimaryImage = imageUrl,
                CreatedAt = now
            };

            ApplyProductFields(product, model);

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

            Auction auction;
            if (model.IsBuyNow)
            {
                var buyNowScheduleStart = now;
                var buyNowLiveStart = buyNowScheduleStart.AddMinutes(1);
                var startingPrice = ResolveBuyNowStartingPrice(model.Price);

                auction = new Auction
                {
                    Product = product,
                    StartingPrice = startingPrice,
                    BidStep = 0.01m,
                    CurrentPrice = model.Price,
                    BuyNowPrice = model.Price,
                    RequiresRegistration = false,
                    RegistrationStartDate = buyNowScheduleStart,
                    RegistrationEndDate = buyNowLiveStart,
                    StartDate = buyNowLiveStart,
                    EndDate = now.AddYears(1),
                    ListingType = ListingTypes.BuyNow,
                    Status = model.Status,
                    VerifiedAt = model.Status is AuctionStatuses.Live or AuctionStatuses.Scheduled or AuctionStatuses.EndingSoon
                        ? now
                        : null,
                    CreatedAt = now
                };
            }
            else
            {
                auction = new Auction
                {
                    Product = product,
                    StartingPrice = model.StartingPrice,
                    BidStep = model.BidStep,
                    CurrentPrice = model.StartingPrice,
                    BuyNowPrice = model.BuyNowPrice,
                    ListingType = ListingTypes.Auction,
                    RequiresRegistration = true,
                    Status = model.Status,
                    RegistrationStartDate = model.RegistrationStartDate,
                    RegistrationEndDate = model.RegistrationEndDate,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    VerifiedAt = model.Status is AuctionStatuses.Live or AuctionStatuses.Scheduled or AuctionStatuses.EndingSoon
                        ? now
                        : null,
                    CreatedAt = now
                };
            }

            _dbContext.Auctions.Add(auction);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, model.IsBuyNow ? "Buy Now listing created successfully." : "Auction created successfully.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ApplyGalleryChangesAsync(
        Product product,
        AuctionFormViewModel model,
        IReadOnlyList<IFormFile> newGalleryFiles,
        DateTime now)
    {
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
    }

    private async Task ApplyDocumentChangesAsync(
        Product product,
        AuctionFormViewModel model,
        DateTime now)
    {
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
    }

    private static void ApplyProductFields(Product product, AuctionFormViewModel model)
    {
        product.Name = model.ProductName.Trim();
        product.ShortDescription = TrimOrNull(model.ShortDescription);
        product.Subtitle = TrimOrNull(model.Subtitle);
        product.DescriptionHtml = model.ProductDescription;
        product.Condition = model.Condition;
        product.Year = model.Year;
        product.SetName = TrimOrNull(model.SetName);
        product.Language = TrimOrNull(model.Language) ?? "English";
        product.CardNumber = TrimOrNull(model.CardNumber);
        product.GradeLabel = TrimOrNull(model.Grade);
        product.CertNumber = TrimOrNull(model.CertificateNumber);
        product.CategoryId = model.CategoryId;
        product.SellerId = model.SellerId;
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

    private async Task<IReadOnlyList<FraudAlertViewModel>> LoadFraudAlertsAsync(int auctionId)
    {
        return await _dbContext.BidFraudAlerts
            .AsNoTracking()
            .Include(alert => alert.User)
            .Include(alert => alert.Reviewer)
            .Where(alert => alert.AuctionId == auctionId)
            .OrderByDescending(alert => alert.CreatedAt)
            .Select(alert => new FraudAlertViewModel
            {
                Id = alert.Id,
                AuctionId = alert.AuctionId,
                BidId = alert.BidId,
                UserId = alert.UserId,
                UserName = alert.User != null ? alert.User.FullName : null,
                AlertType = alert.AlertType,
                Severity = alert.Severity,
                Message = alert.Message,
                Status = alert.Status,
                CreatedAt = alert.CreatedAt,
                ReviewedBy = alert.ReviewedBy,
                ReviewerName = alert.Reviewer != null ? alert.Reviewer.FullName : null,
                ReviewedAt = alert.ReviewedAt
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<AdminWinnerNonPaymentLogViewModel>> LoadWinnerNonPaymentLogsAsync(int auctionId)
    {
        return await _dbContext.WinnerNonPaymentLogs
            .AsNoTracking()
            .Where(log => log.AuctionId == auctionId)
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new AdminWinnerNonPaymentLogViewModel
            {
                Id = log.Id,
                Action = log.Action,
                Details = log.Details,
                DefaultingUserId = log.DefaultingUserId,
                ForfeitedAmount = log.ForfeitedAmount,
                SecondChanceUserId = log.SecondChanceUserId,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<AdminForfeitedDepositViewModel>> LoadForfeitedDepositsAsync(int auctionId)
    {
        return await _dbContext.AuctionRegistrationDeposits
            .AsNoTracking()
            .Include(deposit => deposit.User)
            .Where(deposit =>
                deposit.AuctionId == auctionId &&
                deposit.Status == AuctionRegistrationDepositStatuses.Forfeited)
            .OrderByDescending(deposit => deposit.ForfeitedAt)
            .Select(deposit => new AdminForfeitedDepositViewModel
            {
                DepositId = deposit.Id,
                UserId = deposit.UserId,
                UserName = deposit.User.FullName,
                Amount = deposit.Amount,
                ForfeitedAt = deposit.ForfeitedAt
            })
            .ToListAsync();
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

    private static string ResolveDocumentType(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "PDF",
            _ => "FILE"
        };
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

        if (!string.IsNullOrWhiteSpace(filter.ListingPhase) &&
            !AllowedListingPhases.Contains(filter.ListingPhase))
        {
            filter.ListingPhase = null;
        }

        if (!string.IsNullOrWhiteSpace(filter.ListingType) &&
            filter.ListingType is not (ListingTypes.Auction or ListingTypes.BuyNow))
        {
            filter.ListingType = null;
        }
    }

    private static void ApplyListingDisplayInfo(AuctionListItemViewModel model, DateTime now)
    {
        var auction = BuildScheduleAuction(
            model.Status,
            model.ListingType,
            model.RegistrationStartDate,
            model.RegistrationEndDate,
            model.StartDate,
            model.EndDate);

        ApplyListingDisplayInfo(
            model.Status,
            auction,
            now,
            phase => model.ListingPhase = phase,
            isPublic => model.IsPubliclyListed = isPublic,
            countdownTarget => model.CountdownTargetDate = countdownTarget,
            countdownKind => model.PhaseCountdownKind = countdownKind,
            timeRemaining => model.TimeRemaining = timeRemaining);
        model.ListingPhaseLabel = FormatListingPhaseLabel(model.ListingPhase);
    }

    private static void ApplyListingDisplayInfo(AuctionDetailViewModel model, DateTime now)
    {
        var auction = BuildScheduleAuction(
            model.Status,
            model.ListingType,
            model.RegistrationStartDate,
            model.RegistrationEndDate,
            model.StartDate,
            model.EndDate);

        ApplyListingDisplayInfo(
            model.Status,
            auction,
            now,
            phase => model.ListingPhase = phase,
            isPublic => model.IsPubliclyListed = isPublic,
            countdownTarget => model.CountdownTargetDate = countdownTarget,
            countdownKind => model.PhaseCountdownKind = countdownKind,
            timeRemaining => model.TimeRemaining = timeRemaining);
        model.ListingPhaseLabel = FormatListingPhaseLabel(model.ListingPhase);
    }

    private static void ApplyListingDisplayInfo(
        string status,
        Auction auction,
        DateTime now,
        Action<string> setPhase,
        Action<bool> setIsPublic,
        Action<DateTime?> setCountdownTarget,
        Action<string> setCountdownKind,
        Action<string> setTimeRemaining)
    {
        if (IsNotListedLifecycleStatus(status))
        {
            setPhase(AuctionListingPhases.NotListed);
            setIsPublic(false);
            setCountdownTarget(null);
            setCountdownKind(string.Empty);
            setTimeRemaining("n/a");
            return;
        }

        if (status is AuctionStatuses.Ended or AuctionStatuses.AwaitingPayment or AuctionStatuses.Completed)
        {
            setPhase(AuctionListingPhases.Ended);
            setIsPublic(false);
            setCountdownTarget(auction.EndDate);
            setCountdownKind("live_end");
            setTimeRemaining("Ended");
            return;
        }

        var phaseInfo = AuctionScheduleHelper.ResolveListingPhase(auction, now);
        setPhase(phaseInfo.Phase);
        setIsPublic(AuctionScheduleHelper.IsPubliclyListed(auction, now));
        setCountdownTarget(phaseInfo.CountdownTarget);
        setCountdownKind(phaseInfo.CountdownKind);
        setTimeRemaining(FormatTimeRemaining(phaseInfo.CountdownTarget, now));
    }

    private static Auction BuildScheduleAuction(
        string status,
        string listingType,
        DateTime registrationStartDate,
        DateTime registrationEndDate,
        DateTime startDate,
        DateTime endDate) =>
        new()
        {
            Status = status,
            ListingType = listingType,
            RequiresRegistration = listingType == ListingTypes.Auction,
            RegistrationStartDate = registrationStartDate,
            RegistrationEndDate = registrationEndDate,
            StartDate = startDate,
            EndDate = endDate
        };

    private static bool IsNotListedLifecycleStatus(string status) =>
        AuctionStatuses.IsConfirming(status) ||
        status is AuctionStatuses.Rejected or AuctionStatuses.Cancelled;

    private static string FormatTimeRemaining(DateTime target, DateTime now)
    {
        target = DateTimeUtilities.AsUtc(target);
        now = DateTimeUtilities.AsUtc(now);

        if (target <= now)
        {
            return "Ended";
        }

        var remaining = target - now;
        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }

        return $"{Math.Max(0, remaining.Minutes)}m";
    }

    private static string FormatListingPhaseLabel(string phase) =>
        phase switch
        {
            AuctionListingPhases.RegistrationOpen => "Registration Open",
            AuctionListingPhases.RegistrationClosed => "Awaiting Live",
            AuctionListingPhases.LiveAuction => "Live Now",
            AuctionListingPhases.LiveEndingSoon => "Ending Soon",
            AuctionListingPhases.Upcoming => "Upcoming",
            AuctionListingPhases.Ended => "Ended",
            AuctionListingPhases.NotListed => "Not listed",
            _ => phase.Replace('_', ' ')
        };

    private static string FormatStatusLabel(string status) =>
        status switch
        {
            AuctionStatuses.Confirming => "Confirming",
            AuctionStatuses.Rejected => "Rejected",
            AuctionStatuses.Scheduled => "Scheduled",
            AuctionStatuses.Live => "Live",
            AuctionStatuses.EndingSoon => "Ending soon",
            AuctionStatuses.Ended => "Ended",
            AuctionStatuses.AwaitingPayment => "Awaiting payment",
            AuctionStatuses.Completed => "Completed",
            AuctionStatuses.Cancelled => "Cancelled",
            _ => status.Replace('_', ' ')
        };

    private static decimal ResolveBuyNowStartingPrice(decimal price) =>
        price <= 0.01m ? 0.01m : price - 0.01m;

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
