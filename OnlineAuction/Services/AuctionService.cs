using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineAuction.Configurations;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class AuctionService : IAuctionService
{
    private const int HomeSectionItemCount = 15;

    private static readonly string[] CuratedBestSellerEmails =
    [
        "giangha@auctionhouse.local",
        "nguyen.hai@auctionhouse.local",
        "viet.anh@auctionhouse.local",
        "dan.long@auctionhouse.local",
        "huu.quan@auctionhouse.local",
        "van.hung@auctionhouse.local"
    ];

    private static readonly string[] CuratedBestSellerUserNames =
    [
        "giangha",
        "nguyen.hai",
        "viet.anh",
        "dan.long",
        "huu.quan",
        "van.hung"
    ];

    private static readonly string[] CuratedBestSellerNames =
    [
        "Nguyễn Giang Hà",
        "Đinh Văn Hải",
        "Phạm Việt Anh",
        "Cậu Đan Long",
        "Nguyễn Hữu Quân",
        "Nguyễn Văn Hưng"
    ];

    private readonly AuctionHouseDbContext _dbContext;
    private readonly PlatformFeeSettings _feeSettings;

    public AuctionService(
        AuctionHouseDbContext dbContext,
        IOptions<PlatformFeeSettings> feeSettings)
    {
        _dbContext = dbContext;
        _feeSettings = feeSettings.Value;
    }

    public HomeViewModel GetHomePage() =>
        GetHomePageAsync().GetAwaiter().GetResult();

    public async Task<HomeViewModel> GetHomePageAsync()
    {
        var auctionEntities = await QueryLiveAuctionEntitiesAsync(ListingTypes.Auction);
        var buyNowEntities = await QueryLiveBuyNowEntitiesAsync();
        var allEntities = auctionEntities.Concat(buyNowEntities).ToList();

        var auctionItems = auctionEntities
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction))
            .ToList();
        var buyNowItems = buyNowEntities
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction, true))
            .ToList();
        var allItems = auctionItems.Concat(buyNowItems).ToList();

        var recommended = PickSection(
            allItems
                .Where(ProductDetailMapper.IsRecommendedDeal)
                .OrderByDescending(item => item.DealLabel == "Great Deal")
                .ThenByDescending(item => item.CurrentPrice),
            HomeSectionItemCount);

        var trendingOnAuction = PickSection(
            auctionItems
                .OrderByDescending(item => item.BidCount)
                .ThenByDescending(item => item.CurrentPrice),
            HomeSectionItemCount);

        var trendingOnBuyNow = PickSection(
            buyNowItems.OrderByDescending(item => item.CurrentPrice),
            HomeSectionItemCount);

        var recentlyAdded = PickSection(
            allEntities
                .OrderByDescending(auction => auction.CreatedAt)
                .Select(auction => ProductDetailMapper.MapToAuctionItem(auction)),
            HomeSectionItemCount);

        var categories = await BuildAuctionCategoriesAsync(allItems);
        var bestSellers = await QueryBestSellersAsync();

        return new HomeViewModel
        {
            Recommended = recommended,
            TrendingOnAuction = trendingOnAuction,
            TrendingOnBuyNow = trendingOnBuyNow,
            RecentlyAdded = recentlyAdded,
            BestSellers = bestSellers,
            Categories = categories,
            VaultPosts = []
        };
    }

    public async Task<AuctionViewModel> GetBuyNowIndexAsync()
    {
        var auctions = await QueryPublicBuyNowListingsAsync();
        return new AuctionViewModel
        {
            Auctions = auctions.ToList(),
            Categories = await BuildAuctionCategoriesAsync(auctions)
        };
    }

    public async Task<AuctionViewModel> GetAuctionIndexAsync(string listingType = ListingTypes.Auction)
    {
        var auctions = await QueryPublicListingsAsync(listingType);
        return new AuctionViewModel
        {
            Auctions = auctions.ToList(),
            Categories = await BuildAuctionCategoriesAsync(auctions)
        };
    }

    public AuctionViewModel GetAuctionIndex(string listingType = ListingTypes.Auction) =>
        GetAuctionIndexAsync(listingType).GetAwaiter().GetResult();

    public async Task<ProductDetailViewModel?> GetProductDetailAsync(int id, int? currentUserId = null)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Seller)
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Product)
                .ThenInclude(p => p.Images)
            .Include(a => a.Product)
                .ThenInclude(p => p.Documents)
            .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction?.Product?.Seller is null)
        {
            return null;
        }

        var registrationCount = await AuctionRegistrationService.CountApprovedRegistrationsAsync(_dbContext, id);

        string? userRegistrationStatus = null;
        string? registrationRejectReason = null;

        if (currentUserId.HasValue)
        {
            var userRegistration = await _dbContext.AuctionRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.AuctionId == id && r.UserId == currentUserId.Value);

            userRegistrationStatus = userRegistration?.Status;
            registrationRejectReason = userRegistration?.RejectReason;
        }

        var sellerId = auction.Product.SellerId;
        var auctionCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(p => p.SellerId == sellerId);

        var successfulSales = await _dbContext.Auctions
            .AsNoTracking()
            .CountAsync(a =>
                a.Product.SellerId == sellerId &&
                a.Status == AuctionStatuses.Completed);

        var seller = ProductDetailMapper.MapSeller(
            auction.Product.Seller,
            auctionCount,
            successfulSales);

        var related = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.Id != id &&
                a.Product.CategoryId == auction.Product.CategoryId &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > DateTime.UtcNow)
            .OrderBy(a => a.EndDate)
            .Take(12)
            .ToListAsync();

        var similarIds = related.Select(a => a.Id).ToList();

        var moreRelated = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.Id != id &&
                !similarIds.Contains(a.Id) &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > DateTime.UtcNow)
            .OrderByDescending(a => a.Bids.Count)
            .ThenByDescending(a => a.CreatedAt)
            .Take(12)
            .ToListAsync();

        var relatedItems = related
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction))
            .ToList();

        var moreRelatedItems = moreRelated
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction))
            .ToList();

        return ProductDetailMapper.MapToViewModel(
            auction,
            seller,
            relatedItems,
            moreRelatedItems,
            currentUserId,
            userRegistrationStatus,
            registrationRejectReason,
            registrationCount,
            _feeSettings);
    }

    public async Task<AuctionItemViewModel?> GetAuctionByIdAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id);

        return auction is null ? null : ProductDetailMapper.MapToAuctionItem(auction);
    }

    public AuctionItemViewModel? GetAuctionById(int id) =>
        GetAuctionByIdAsync(id).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<AuctionItemViewModel>> GetAllAuctionsAsync()
    {
        var auctions = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return auctions
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction))
            .ToList();
    }

    public IReadOnlyList<AuctionItemViewModel> GetAllAuctions() =>
        GetAllAuctionsAsync().GetAwaiter().GetResult();

    private async Task<IReadOnlyList<AuctionItemViewModel>> QueryPublicBuyNowListingsAsync()
    {
        var auctions = await QueryLiveBuyNowEntitiesAsync();
        return auctions
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction, true))
            .ToList();
    }

    private async Task<IReadOnlyList<AuctionItemViewModel>> QueryPublicListingsAsync(string listingType)
    {
        var auctions = await QueryLiveAuctionEntitiesAsync(listingType);
        return auctions
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(auction => ProductDetailMapper.MapToAuctionItem(auction))
            .ToList();
    }

    private async Task<List<Auction>> QueryLiveBuyNowEntitiesAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.BuyNowPrice != null &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > now)
            .ToListAsync();
    }

    private async Task<List<Auction>> QueryLiveAuctionEntitiesAsync(string listingType)
    {
        var now = DateTime.UtcNow;

        var auctions = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.ListingType == listingType &&
                (
                    (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                    a.EndDate > now
                    ||
                    a.Status == AuctionStatuses.Scheduled &&
                    a.RegistrationStartDate <= now &&
                    a.StartDate > now
                ))
            .ToListAsync();

        return auctions;
    }

    private async Task<List<SellerViewModel>> QueryBestSellersAsync()
    {
        var now = DateTime.UtcNow;
        var sellers = await _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Products)
                .ThenInclude(product => product.Auctions)
            .Where(user =>
                user.Status == UserStatus.Active &&
                ((user.Email != null && CuratedBestSellerEmails.Contains(user.Email)) ||
                 (user.UserName != null && CuratedBestSellerUserNames.Contains(user.UserName))))
            .ToListAsync();

        var sellersByEmail = sellers
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .ToDictionary(user => user.Email!, StringComparer.OrdinalIgnoreCase);

        var sellersByUserName = sellers
            .Where(user => !string.IsNullOrWhiteSpace(user.UserName))
            .ToDictionary(user => user.UserName!, StringComparer.OrdinalIgnoreCase);

        var result = new List<SellerViewModel>(CuratedBestSellerEmails.Length);

        for (var index = 0; index < CuratedBestSellerEmails.Length; index++)
        {
            var email = CuratedBestSellerEmails[index];
            var userName = CuratedBestSellerUserNames[index];
            var displayName = CuratedBestSellerNames[index];

            ApplicationUser? user = null;
            if (!sellersByEmail.TryGetValue(email, out user))
            {
                sellersByUserName.TryGetValue(userName, out user);
            }
            {
                result.Add(new SellerViewModel
                {
                    FullName = displayName,
                    Username = displayName,
                    AvatarUrl = $"/admin/images/user/user-{((index % 37) + 1):D2}.jpg"
                });
                continue;
            }

            var auctions = user.Products.SelectMany(product => product.Auctions).ToList();
            var liveCount = auctions.Count(auction =>
                (auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon) &&
                auction.EndDate > now);
            var completedCount = auctions.Count(auction => auction.Status == AuctionStatuses.Completed);

            var seller = ProductDetailMapper.MapSeller(user, liveCount, completedCount);
            if (string.IsNullOrWhiteSpace(seller.FullName))
            {
                seller.FullName = displayName;
            }

            result.Add(seller);
        }

        return result;
    }

    private static List<AuctionItemViewModel> PickSection(
        IEnumerable<AuctionItemViewModel> source,
        int count) =>
        source.Take(count).ToList();

    private async Task<List<CategoryViewModel>> BuildAuctionCategoriesAsync(
        IReadOnlyList<AuctionItemViewModel> auctions)
    {
        var countsByName = auctions
            .Where(item => !string.IsNullOrWhiteSpace(item.Category))
            .GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var dbCategoryNames = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => category.Name)
            .ToListAsync();

        var categoryNames = dbCategoryNames.Count > 0
            ? dbCategoryNames
            : countsByName.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();

        return categoryNames
            .Select(name => new CategoryViewModel
            {
                Name = name,
                ItemCount = countsByName.GetValueOrDefault(name, 0),
                ImageUrl = CategoryImages.GetImageUrl(name),
                DisplayCount = countsByName.TryGetValue(name, out var count)
                    ? $"{count} Items"
                    : "0 Items"
            })
            .ToList();
    }
}
