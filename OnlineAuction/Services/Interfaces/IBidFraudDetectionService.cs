namespace OnlineAuction.Services.Interfaces;

public sealed record BidFraudGateResult(
    bool IsAllowed,
    string? BlockMessage = null,
    bool AppliedShadowBan = false,
    string? TriggeredAlertType = null,
    string? TriggeredSeverity = null);

public interface IBidFraudDetectionService
{
    /// <summary>
    /// Pre-bid gate: may reject or shadow-ban before a bid row is inserted.
    /// Always creates fraud alerts when rules fire (admin-visible).
    /// </summary>
    Task<BidFraudGateResult> EvaluatePreBidAsync(
        int auctionId,
        int bidderId,
        decimal amount,
        decimal previousPrice,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task EvaluatePostBidAsync(
        int auctionId,
        long bidId,
        int bidderId,
        decimal previousPrice,
        CancellationToken cancellationToken = default);
}
