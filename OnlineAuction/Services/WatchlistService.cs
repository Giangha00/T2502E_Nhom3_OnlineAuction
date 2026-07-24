using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class WatchlistService : IWatchlistService
{
    private readonly AuctionHouseDbContext _db;

    public WatchlistService(AuctionHouseDbContext db)
    {
        _db = db;
    }

    public async Task<WatchlistToggleResult> ToggleAsync(
        int userId,
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var auction = await _db.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == auctionId && a.DeletedAt == null, cancellationToken);

        if (auction is null)
        {
            throw new InvalidOperationException("Auction not found.");
        }

        var existing = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId, cancellationToken);

        if (existing is not null)
        {
            _db.WatchlistItems.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            return new WatchlistToggleResult
            {
                IsWatched = false,
                Count = await GetCountAsync(userId, cancellationToken)
            };
        }

        var isOwner = auction.Product.SellerId == userId;
        if (!isOwner &&
            (AuctionStatuses.IsConfirming(auction.Status) ||
             auction.Status is AuctionStatuses.Rejected or AuctionStatuses.Cancelled))
        {
            throw new InvalidOperationException("This listing is not available to watch.");
        }

        _db.WatchlistItems.Add(new WatchlistItem
        {
            UserId = userId,
            AuctionId = auctionId,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new WatchlistToggleResult
        {
            IsWatched = true,
            Count = await GetCountAsync(userId, cancellationToken)
        };
    }

    public Task<bool> IsWatchedAsync(int userId, int auctionId, CancellationToken cancellationToken = default) =>
        _db.WatchlistItems
            .AsNoTracking()
            .AnyAsync(w => w.UserId == userId && w.AuctionId == auctionId, cancellationToken);

    public async Task<IReadOnlySet<int>> GetWatchedAuctionIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.AuctionId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public Task<int> GetCountAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.WatchlistItems
            .AsNoTracking()
            .CountAsync(w => w.UserId == userId, cancellationToken);

    public async Task<List<AuctionItemViewModel>> GetItemsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var auctionIds = await _db.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => w.AuctionId)
            .ToListAsync(cancellationToken);

        if (auctionIds.Count == 0)
        {
            return [];
        }

        var auctions = await _db.Auctions
            .AsNoTracking()
            .Where(a =>
                auctionIds.Contains(a.Id) &&
                a.DeletedAt == null &&
                a.Status != AuctionStatuses.Cancelled &&
                a.Status != AuctionStatuses.Confirming &&
                a.Status != AuctionStatuses.LegacyPendingReview &&
                a.Status != AuctionStatuses.Rejected)
            .Include(a => a.Product)
                .ThenInclude(p => p.Category)
            .Include(a => a.Bids)
            .ToListAsync(cancellationToken);

        var orderMap = auctionIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        return auctions
            .OrderBy(a => orderMap.GetValueOrDefault(a.Id, int.MaxValue))
            .Select(a => ProductDetailMapper.MapToAuctionItem(
                a,
                a.ListingType == ListingTypes.BuyNow))
            .ToList();
    }
}
