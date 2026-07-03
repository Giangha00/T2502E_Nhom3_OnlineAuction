namespace OnlineAuction.Services.Interfaces;

public interface IBidFraudDetectionService
{
    Task EvaluateAsync(
        int auctionId,
        long bidId,
        int bidderId,
        decimal previousPrice,
        CancellationToken cancellationToken = default);
}
