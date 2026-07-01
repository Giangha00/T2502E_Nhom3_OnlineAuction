namespace OnlineAuction.Services.Interfaces;

public sealed record BidRateLimitResult(bool IsAllowed, string? Reason = null);

public interface IBidRateLimitService
{
    Task<BidRateLimitResult> CheckAsync(
        int auctionId,
        int bidderId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
