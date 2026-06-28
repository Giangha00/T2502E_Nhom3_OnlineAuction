using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IWatchlistService
{
    Task<WatchlistToggleResult> ToggleAsync(int userId, int auctionId, CancellationToken cancellationToken = default);

    Task<bool> IsWatchedAsync(int userId, int auctionId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> GetWatchedAuctionIdsAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(int userId, CancellationToken cancellationToken = default);

    Task<List<AuctionItemViewModel>> GetItemsAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class WatchlistToggleResult
{
    public bool IsWatched { get; init; }

    public int Count { get; init; }
}
