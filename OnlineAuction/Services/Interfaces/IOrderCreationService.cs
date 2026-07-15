using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services.Interfaces;

public interface IOrderCreationService
{
    Task<int> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default);

    Task<int?> CreatePendingPaymentOrderForAuctionAsync(
        int auctionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an auction-win order using the current winning bid without opening a nested transaction.
    /// Caller is responsible for SaveChanges.
    /// </summary>
    Task<bool> TryCreatePendingPaymentOrderWithinUnitOfWorkAsync(
        int auctionId,
        DateTime now,
        int paymentDeadlineHours,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CreatePendingPaymentOrderForBuyNowAsync(
        int auctionId,
        int buyerId,
        CancellationToken cancellationToken = default);
}
