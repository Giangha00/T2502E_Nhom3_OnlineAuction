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

    public AuctionViewModel GetAuctionIndex()
    {
        return new AuctionViewModel
        {
            Categories = MockAuctionData.GetCategories(),
            Auctions = MockAuctionData.GetAllAuctions()
        };
    }

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
            .Where(a =>
                a.Id != id &&
                a.Product.CategoryId == auction.Product.CategoryId &&
                (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon))
            .OrderBy(a => a.EndDate)
            .Take(4)
            .ToListAsync();

        var relatedItems = related
            .Select(ProductDetailMapper.MapToAuctionItem)
            .ToList();

        return ProductDetailMapper.MapToViewModel(auction, seller, relatedItems);
    }

    public AuctionItemViewModel? GetAuctionById(int id) =>
        MockAuctionData.GetAuctionById(id);

    public IReadOnlyList<AuctionItemViewModel> GetAllAuctions() =>
        MockAuctionData.GetAllAuctions();
}
