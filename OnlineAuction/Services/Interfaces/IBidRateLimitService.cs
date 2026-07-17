namespace OnlineAuction.Services.Interfaces;

public sealed record BidRateLimitResult(
    bool IsAllowed,
    string? Reason = null,
    bool RequiresChallenge = false,
    int UserCount = 0,
    int AuctionCount = 0,
    int IpCount = 0);

public interface IBidRateLimitService
{
    Task<BidRateLimitResult> CheckAsync(
        int auctionId,
        int bidderId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
