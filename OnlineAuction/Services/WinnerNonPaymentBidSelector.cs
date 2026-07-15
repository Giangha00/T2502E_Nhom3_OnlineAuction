using OnlineAuction.Entities;

namespace OnlineAuction.Services;

public static class WinnerNonPaymentBidSelector
{
    public static Bid? SelectRunnerUp(
        IEnumerable<Bid> bids,
        IReadOnlyCollection<int> excludedBidderIds)
    {
        return bids
            .Where(bid =>
                bid.DeletedAt == null &&
                !bid.IsFlagged &&
                !excludedBidderIds.Contains(bid.BidderId))
            .OrderByDescending(bid => bid.Amount)
            .ThenByDescending(bid => bid.PlacedAt)
            .FirstOrDefault();
    }
}
