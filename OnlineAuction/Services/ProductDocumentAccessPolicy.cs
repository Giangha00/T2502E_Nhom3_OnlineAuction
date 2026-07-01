using OnlineAuction.Entities;

namespace OnlineAuction.Services;

public static class ProductDocumentAccessPolicy
{
    public static readonly string[] PublicAuctionStatuses =
    [
        AuctionStatuses.Live,
        AuctionStatuses.EndingSoon,
        AuctionStatuses.Ended,
        AuctionStatuses.AwaitingPayment,
        AuctionStatuses.Completed
    ];

    public static bool IsPublicAuctionStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        PublicAuctionStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static bool CanAnonymousDownload(IEnumerable<string> auctionStatuses) =>
        auctionStatuses.Any(IsPublicAuctionStatus);
}
