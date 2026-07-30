using Microsoft.Extensions.Localization;
using OnlineAuction.Entities;

namespace OnlineAuction.Areas.Admin.Services;

internal static class AdminAuctionLifecycleHelper
{
    private static readonly HashSet<string> LiveLikeStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    private static readonly HashSet<string> CancellableStatuses =
    [
        AuctionStatuses.Confirming,
        AuctionStatuses.Rejected,
        AuctionStatuses.Scheduled,
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon
    ];

    public static bool IsLiveLikeStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && LiveLikeStatuses.Contains(status);

    public static bool IsScheduleLockedStatus(string? status) => IsLiveLikeStatus(status);

    public static bool CanCancelStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && CancellableStatuses.Contains(status);

    public static void ApplyMutationFlags(
        int bidCount,
        int orderCount,
        string status,
        Action<bool> setCanDelete,
        Action<bool> setCanCancel,
        Action<string?> setBlockReason,
        IStringLocalizer<SharedResource> localizer)
    {
        if (status == AuctionStatuses.Cancelled || status == AuctionStatuses.Completed)
        {
            setCanDelete(false);
            setCanCancel(false);
            setBlockReason(localizer["AdminMsg_Auction_EndedOrCancelled"].Value);
            return;
        }

        if (orderCount > 0)
        {
            setCanDelete(false);
            setCanCancel(false);
            setBlockReason(localizer["AdminMsg_Auction_DeleteBlockedOrder"].Value);
            return;
        }

        if (bidCount > 0)
        {
            setCanDelete(false);
            setCanCancel(CanCancelStatus(status));
            setBlockReason(localizer.GetString("AdminMsg_Auction_DeleteBlockedBids", bidCount).Value);
            return;
        }

        setCanDelete(true);
        setCanCancel(CanCancelStatus(status));
        setBlockReason(null);
    }

    public static string BuildDeleteBlockedMessage(
        int bidCount,
        int orderCount,
        IStringLocalizer<SharedResource> localizer)
    {
        if (orderCount > 0)
        {
            return localizer["AdminMsg_Auction_DeleteBlockedOrder"].Value;
        }

        if (bidCount > 0)
        {
            return localizer.GetString("AdminMsg_Auction_DeleteBlockedBids", bidCount).Value;
        }

        return localizer["AdminMsg_Auction_CannotDelete"].Value;
    }

    public static string BuildBulkSkipReason(int auctionId, int bidCount, int orderCount)
    {
        if (orderCount > 0)
        {
            return $"#{auctionId} (linked order)";
        }

        if (bidCount > 0)
        {
            return $"#{auctionId} ({bidCount} bids)";
        }

        return $"#{auctionId}";
    }

    public static string BuildBulkDeleteSummary(
        int deletedCount,
        int requestedCount,
        IReadOnlyList<string> skipped,
        IStringLocalizer<SharedResource> localizer)
    {
        if (skipped.Count == 0)
        {
            return localizer.GetString("AdminMsg_Auction_BulkDeleted", deletedCount, requestedCount).Value;
        }

        return localizer.GetString(
            "AdminMsg_Auction_BulkDeletedWithSkip",
            deletedCount,
            requestedCount,
            string.Join(", ", skipped)).Value;
    }
}
