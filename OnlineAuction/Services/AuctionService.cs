using Microsoft.EntityFrameworkCore;
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

    private readonly AuctionHouseDbContext _dbContext;

    public AuctionService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public HomeViewModel GetHomePage() =>
        GetHomePageAsync().GetAwaiter().GetResult();

    public async Task<HomeViewModel> GetHomePageAsync()
    {
        var auctionEntities = await QueryLiveAuctionEntitiesAsync(ListingTypes.Auction);
        var buyNowEntities = await QueryLiveAuctionEntitiesAsync(ListingTypes.BuyNow);
        var allEntities = auctionEntities.Concat(buyNowEntities).ToList();

        var auctionItems = auctionEntities.Select(ProductDetailMapper.MapToAuctionItem).ToList();
        var buyNowItems = buyNowEntities.Select(ProductDetailMapper.MapToAuctionItem).ToList();
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
                .Select(ProductDetailMapper.MapToAuctionItem),
            HomeSectionItemCount);

        var categories = await BuildAuctionCategoriesAsync(allItems);
        var bestSellers = await QueryBestSellersAsync();

        return new HomeViewModel
        {
            Recommended = WithFallback(recommended, MockAuctionData.GetRecommended),
            TrendingOnAuction = WithFallback(trendingOnAuction, MockAuctionData.GetTrendingOnAuction),
            TrendingOnBuyNow = WithFallback(trendingOnBuyNow, MockAuctionData.GetTrendingOnBuyNow),
            RecentlyAdded = WithFallback(recentlyAdded, MockAuctionData.GetRecentlyAdded),
            BestSellers = WithFallback(bestSellers, MockAuctionData.GetBestSellers),
            Categories = categories.Count > 0 ? categories : MockAuctionData.GetCategories(),
            VaultPosts = MockAuctionData.GetVaultPosts()
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
            .Take(4)
            .ToListAsync();

        var relatedItems = related
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();

        return ProductDetailMapper.MapToViewModel(
            auction,
            seller,
            relatedItems,
            currentUserId,
            userRegistrationStatus,
            registrationRejectReason,
            registrationCount);
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
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();
    }

    public IReadOnlyList<AuctionItemViewModel> GetAllAuctions() =>
        GetAllAuctionsAsync().GetAwaiter().GetResult();

    private async Task<IReadOnlyList<AuctionItemViewModel>> QueryPublicListingsAsync(string listingType)
    {
        var auctions = await QueryLiveAuctionEntitiesAsync(listingType);
        return auctions
            .OrderByDescending(auction => auction.CreatedAt)
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();
    }

    private async Task<List<Auction>> QueryLiveAuctionEntitiesAsync(string listingType)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.ListingType == listingType &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > now)
            .ToListAsync();
    }

    private async Task<List<SellerViewModel>> QueryBestSellersAsync(int count = 5)
    {
        var now = DateTime.UtcNow;
        var sellers = await _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Products)
                .ThenInclude(product => product.Auctions)
            .Where(user => user.Status == UserStatus.Active && user.Role == UserRole.User)
            .ToListAsync();

        return sellers
            .Select(user =>
            {
                var auctions = user.Products.SelectMany(product => product.Auctions).ToList();
                var liveCount = auctions.Count(auction =>
                    (auction.Status == AuctionStatuses.Live || auction.Status == AuctionStatuses.EndingSoon) &&
                    auction.EndDate > now);
                var completedCount = auctions.Count(auction => auction.Status == AuctionStatuses.Completed);

                return ProductDetailMapper.MapSeller(user, liveCount, completedCount);
            })
            .Where(seller => seller.AuctionCount > 0)
            .OrderByDescending(seller => seller.AuctionCount)
            .ThenByDescending(seller => seller.SuccessfulSales)
            .Take(count)
            .ToList();
    }

    private static List<AuctionItemViewModel> PickSection(
        IEnumerable<AuctionItemViewModel> source,
        int count) =>
        source.Take(count).ToList();

    private static List<T> WithFallback<T>(List<T> primary, Func<List<T>> fallback) =>
        primary.Count > 0 ? primary : fallback();

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
                ImageUrl = MockAuctionData.GetCategoryImageUrl(name),
                DisplayCount = countsByName.TryGetValue(name, out var count)
                    ? $"{count} Items"
                    : "0 Items"
            })
            .ToList();
    }
}
