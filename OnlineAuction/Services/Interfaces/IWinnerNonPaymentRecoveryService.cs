using OnlineAuction.Entities;

namespace OnlineAuction.Services.Interfaces;

public interface IWinnerNonPaymentRecoveryService
{
    Task ProcessExpiredAuctionWinOrderAsync(
        AuctionOrder cancelledOrder,
        DateTime now,
        CancellationToken cancellationToken = default);
}
