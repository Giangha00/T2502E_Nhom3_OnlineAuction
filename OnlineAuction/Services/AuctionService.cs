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
        var now = DateTime.UtcNow;
        var auctions = _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Product)
                .ThenInclude(p => p.Seller)
            .Include(a => a.Bids)
            .Where(a =>
                a.ListingType == ListingTypes.Auction &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon) &&
                a.EndDate > now)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        var allAuctions = auctions
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();

        var endingSoon = allAuctions
            .Where(a => a.Status == "Ending Soon")
            .Take(8)
            .ToList();

        var hotAuctions = allAuctions
            .Where(a => a.IsHot)
            .OrderByDescending(a => a.CurrentPrice)
            .Take(8)
            .ToList();

        if (hotAuctions.Count == 0)
        {
            hotAuctions = allAuctions.Take(8).ToList();
        }

        var bestSellers = auctions
            .Where(a => a.Product.Seller is not null)
            .GroupBy(a => a.Product.Seller)
            .Select(group => ProductDetailMapper.MapSeller(
                group.Key,
                group.Select(a => a.ProductId).Distinct().Count(),
                group.Count(a => a.Status == AuctionStatuses.Completed)))
            .OrderByDescending(seller => seller.AuctionCount)
            .Take(5)
            .ToList();

        return new HomeViewModel
        {
            HotAuctions = hotAuctions,
            FeaturedAuctions = allAuctions.Take(12).ToList(),
            EndingSoonAuctions = endingSoon,
            WonAuctions = [],
            BestSellers = bestSellers,
            Categories = ProductDetailMapper.MapCategories(allAuctions),
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
