using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IUserAccountService
{
    Task<List<AuctionItemViewModel>> GetUserBidsAsync(
        int userId,
        string tab = "active",
        CancellationToken cancellationToken = default);

    Task<List<AuctionItemViewModel>> GetUserOffersAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<AuctionItemViewModel>> GetUserSubmissionsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
