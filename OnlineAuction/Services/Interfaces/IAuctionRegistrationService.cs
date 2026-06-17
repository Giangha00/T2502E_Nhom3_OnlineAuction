using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IAuctionRegistrationService
{
    Task<AuctionRegistrationResult> RegisterAsync(int auctionId, int userId);

    Task<AuctionRegistrationResult> CancelRegistrationAsync(int auctionId, int userId);

    Task<string?> GetBidBlockMessageAsync(int auctionId, int userId, bool requiresRegistration);
}
