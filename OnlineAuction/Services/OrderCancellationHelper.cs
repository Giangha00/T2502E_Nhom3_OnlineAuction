using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;

namespace OnlineAuction.Services;

public static class OrderCancellationHelper
{
    public static async Task ApplyCancellationSideEffectsAsync(
        AuctionHouseDbContext dbContext,
        AuctionOrder order,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var auctionId = order.Items.FirstOrDefault()?.AuctionId;
        if (!auctionId.HasValue)
        {
            return;
        }

        var auction = await dbContext.Auctions
            .FirstOrDefaultAsync(item => item.Id == auctionId.Value, cancellationToken);

        if (auction is null)
        {
            return;
        }

        var orderSource = OrderCheckoutSelection.ResolveOrderSource(order);

        if (orderSource == OrderSources.BuyNow)
        {
            if (DateTimeUtilities.IsInFutureUtc(auction.EndDate))
            {
                auction.Status = AuctionStatuses.Live;
            }
            else
            {
                auction.Status = AuctionStatuses.Ended;
            }

            auction.WinnerId = null;
        }
        // auction_win non-payment is handled by WinnerNonPaymentRecoveryService.

        auction.UpdatedAt = now;
    }

    public static async Task MarkAuctionsCompletedAfterPaymentAsync(
        AuctionHouseDbContext dbContext,
        IEnumerable<AuctionOrder> orders,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var auctionIds = orders
            .SelectMany(order => order.Items)
            .Select(item => item.AuctionId)
            .Distinct()
            .ToList();

        if (auctionIds.Count == 0)
        {
            return;
        }

        var auctions = await dbContext.Auctions
            .Where(auction => auctionIds.Contains(auction.Id))
            .ToListAsync(cancellationToken);

        foreach (var auction in auctions)
        {
            auction.Status = AuctionStatuses.Completed;
            auction.UpdatedAt = now;
        }
    }
}
