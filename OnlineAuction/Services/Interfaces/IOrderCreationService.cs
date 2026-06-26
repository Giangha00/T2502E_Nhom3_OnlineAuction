namespace OnlineAuction.Services.Interfaces;

public interface IOrderCreationService
{
    Task<int> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default);

    Task<int?> CreatePendingPaymentOrderForAuctionAsync(
        int auctionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CreatePendingPaymentOrderForBuyNowAsync(
        int auctionId,
        int buyerId,
        CancellationToken cancellationToken = default);
}
