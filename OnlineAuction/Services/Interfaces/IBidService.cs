using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IBidService
{
    Task<PlaceBidResult> PlaceBidAsync(int auctionId, int bidderId, decimal amount);

    Task<AuctionBidStateViewModel?> GetBidStateAsync(int auctionId, CancellationToken cancellationToken = default);

    Task<AuctionBidHistoryPageViewModel?> GetAuctionBidHistoryPageAsync(
        int auctionId,
        int page = 1,
        CancellationToken cancellationToken = default);
}
