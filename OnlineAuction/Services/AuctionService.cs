using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionHouseDbContext _dbContext;

    public AuctionService(AuctionHouseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public HomeViewModel GetHomePage()
    {
        var allAuctions = MockAuctionData.GetAllAuctions();
        var endingSoon = allAuctions.Where(a => a.Status == "Ending Soon").ToList();

        return new HomeViewModel
        {
            HotAuctions = MockAuctionData.GetHotAuctions(),
            FeaturedAuctions = MockAuctionData.GetFeaturedAuctions(),
            EndingSoonAuctions = endingSoon,
            WonAuctions = MockAuctionData.GetWonAuctions(),
            BestSellers = MockAuctionData.GetBestSellers(),
            Categories = MockAuctionData.GetCategories(),
            VaultPosts = MockAuctionData.GetVaultPosts(),
            TotalLiveAuctions = allAuctions.Count,
            EndingSoonCount = endingSoon.Count
        };
    }

    public async Task<AuctionViewModel> GetAuctionIndexAsync(string listingType = ListingTypes.Auction)
    {
        var auctions = await QueryPublicListingsAsync(listingType);
        return new AuctionViewModel
        {
            Auctions = auctions.ToList(),
            Categories = ProductDetailMapper.MapCategories(auctions)
        };
    }

    public AuctionViewModel GetAuctionIndex(string listingType = ListingTypes.Auction) =>
        GetAuctionIndexAsync(listingType).GetAwaiter().GetResult();

    public async Task<ProductDetailViewModel?> GetProductDetailAsync(int id)
    {
        var auction = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Seller)
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction?.Product?.Seller is null)
        {
            return null;
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

        return ProductDetailMapper.MapToViewModel(auction, seller, relatedItems);
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
        var now = DateTime.UtcNow;

        var auctions = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .Where(a =>
                a.ListingType == listingType &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > now)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return auctions
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();
    }
}
