using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class UserAccountService : IUserAccountService
{
    private readonly AuctionHouseDbContext _db;

    public UserAccountService(AuctionHouseDbContext db)
    {
        _db = db;
    }

    public async Task<List<AuctionItemViewModel>> GetUserBidsAsync(
        int userId,
        string tab = "active",
        CancellationToken cancellationToken = default)
    {
        var normalizedTab = tab.ToLowerInvariant() switch
        {
            "past" => "past",
            _ => "active"
        };

        var activeStatuses = new[]
        {
            AuctionStatuses.Live,
            AuctionStatuses.EndingSoon,
            AuctionStatuses.Scheduled
        };

        var bidAuctionIds = await _db.Bids
            .AsNoTracking()
            .Where(b => b.BidderId == userId)
            .Select(b => b.AuctionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (bidAuctionIds.Count == 0)
        {
            return [];
        }

        var auctions = await _db.Auctions
            .AsNoTracking()
            .Where(a =>
                bidAuctionIds.Contains(a.Id) &&
                a.DeletedAt == null &&
                a.Status != AuctionStatuses.Cancelled &&
                (normalizedTab == "active"
                    ? activeStatuses.Contains(a.Status)
                    : !activeStatuses.Contains(a.Status)))
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .OrderByDescending(a => a.EndDate)
            .ToListAsync(cancellationToken);

        return auctions
            .Select(a => ProductDetailMapper.MapToAuctionItem(a))
            .ToList();
    }

    public async Task<List<AuctionItemViewModel>> GetUserOffersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var auctionIds = await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.BuyerId == userId &&
                o.DeletedAt == null &&
                o.Status == OrderStatuses.PendingPayment &&
                o.OrderSource == OrderSources.BuyNow)
            .SelectMany(o => o.Items)
            .Select(i => i.AuctionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (auctionIds.Count == 0)
        {
            return [];
        }

        var auctions = await _db.Auctions
            .AsNoTracking()
            .Where(a => auctionIds.Contains(a.Id) && a.DeletedAt == null)
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return auctions
            .Select(a => ProductDetailMapper.MapToAuctionItem(a, forBuyNowCatalog: true))
            .ToList();
    }

    public async Task<List<AuctionItemViewModel>> GetUserSubmissionsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var submissionStatuses = new[]
        {
            AuctionStatuses.PendingReview,
            AuctionStatuses.Rejected,
            AuctionStatuses.Scheduled
        };

        var auctions = await _db.Auctions
            .AsNoTracking()
            .Where(a =>
                a.Product.SellerId == userId &&
                a.DeletedAt == null &&
                submissionStatuses.Contains(a.Status))
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .ToListAsync(cancellationToken);

        return auctions
            .Select(a =>
            {
                var item = ProductDetailMapper.MapToAuctionItem(
                    a,
                    a.ListingType == ListingTypes.BuyNow);
                item.RejectReason = a.RejectReason;
                return item;
            })
            .ToList();
    }
}
