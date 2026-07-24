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

    public static void ApplyMutationFlags(
        int bidCount,
        int orderCount,
        string status,
        Action<bool> setCanDelete,
        Action<bool> setCanCancel,
        Action<string?> setBlockReason)
    {
        if (status == AuctionStatuses.Cancelled || status == AuctionStatuses.Completed)
        {
            setCanDelete(false);
            setCanCancel(false);
            setBlockReason("Phiên đã kết thúc hoặc đã hủy.");
            return;
        }

        if (orderCount > 0)
        {
            setCanDelete(false);
            setCanCancel(false);
            setBlockReason("Không xóa được — phiên đã liên kết order.");
            return;
        }

        if (bidCount > 0)
        {
            setCanDelete(false);
            setCanCancel(CanCancelStatus(status));
            setBlockReason($"Không xóa được — đã có {bidCount} bid. Hãy Cancel thay vì Delete.");
            return;
        }

        setCanDelete(true);
        setCanCancel(CanCancelStatus(status));
        setBlockReason(null);
    }

    public static string BuildDeleteBlockedMessage(int bidCount, int orderCount)
    {
        if (orderCount > 0)
        {
            return "Không xóa được — phiên đã liên kết order.";
        }

        if (bidCount > 0)
        {
            return $"Không xóa được — đã có {bidCount} bid. Hãy Cancel thay vì Delete.";
        }

        return "Không thể xóa phiên đấu giá này.";
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

    public static string BuildBulkDeleteSummary(int deletedCount, int requestedCount, IReadOnlyList<string> skipped)
    {
        if (skipped.Count == 0)
        {
            return $"Đã xóa {deletedCount}/{requestedCount} phiên đấu giá.";
        }

        return $"Đã xóa {deletedCount}/{requestedCount}; bỏ qua: {string.Join(", ", skipped)}.";
    }

    public static bool CanCancelStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && CancellableStatuses.Contains(status);
}
