using OnlineAuction.Entities;
using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IListingFeeService
{
    decimal CalculateListingFee(decimal startingPrice);

    string BuildPreviewDescription(decimal startingPrice);

    Task<ListingFeeCollectionResult> CollectListingFeeAsync(
        Auction auction,
        int adminUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPaidListingFeeAsync(int auctionId, CancellationToken cancellationToken = default);
}
