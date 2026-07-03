using OnlineAuction.Areas.Admin.ViewModels.Auctions;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.Services;

public static class AdminBidHistoryMapper
{
    public static IReadOnlyList<AdminBidHistoryItemViewModel> Map(
        IEnumerable<Bid> bids,
        int skip)
    {
        return bids
            .Select((bid, index) => new AdminBidHistoryItemViewModel
            {
                RowNumber = skip + index + 1,
                BidderId = bid.BidderId,
                BidderName = ResolveBidderName(bid.Bidder),
                BidderEmail = bid.Bidder.Email ?? string.Empty,
                Amount = bid.Amount,
                PlacedAt = bid.PlacedAt,
                BidType = bid.BidType,
                Status = bid.IsWinning ? "WINNING" : "OUTBID",
                IsWinning = bid.IsWinning,
                IsFlagged = bid.IsFlagged,
                FlagReason = bid.FlagReason
            })
            .ToList();
    }

    private static string ResolveBidderName(ApplicationUser bidder)
    {
        if (!string.IsNullOrWhiteSpace(bidder.FullName))
        {
            return bidder.FullName;
        }

        return bidder.UserName ?? bidder.Email ?? "Bidder";
    }
}
