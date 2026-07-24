using OnlineAuction.Entities;
using OnlineAuction.Models;

namespace OnlineAuction.Helpers;

public static class SellerListingActions
{
    public static bool CanManage(AuctionItemViewModel listing)
    {
        if (listing.BidCount > 0)
        {
            return false;
        }

        var status = listing.Status?.Trim() ?? string.Empty;
        if (AuctionStatuses.IsConfirming(status)
            || string.Equals(status, AuctionStatuses.Rejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, AuctionStatuses.Scheduled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Confirming", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(status, AuctionStatuses.Live, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Live", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, AuctionStatuses.EndingSoon, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Ending Soon", StringComparison.OrdinalIgnoreCase))
        {
            return listing.EndDate is null || DateTimeUtilities.IsInFutureUtc(listing.EndDate.Value);
        }

        return false;
    }
}
