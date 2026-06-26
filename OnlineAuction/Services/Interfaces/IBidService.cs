using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IBidService
{
    Task<PlaceBidResult> PlaceBidAsync(int auctionId, int bidderId, decimal amount);

    Task<AuctionBidStateViewModel?> GetBidStateAsync(int auctionId, CancellationToken cancellationToken = default);
}
