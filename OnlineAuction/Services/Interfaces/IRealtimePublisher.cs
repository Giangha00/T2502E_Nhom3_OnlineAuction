using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface IRealtimePublisher
{
    Task SendNotificationToUserAsync(
        int userId,
        NotificationItemViewModel notification,
        int unreadCount,
        CancellationToken cancellationToken = default);

    Task SendOrderCountToUserAsync(
        int userId,
        int orderCount,
        CancellationToken cancellationToken = default);

    Task SendBidUpdateAsync(
        int auctionId,
        AuctionBidStateViewModel state,
        CancellationToken cancellationToken = default);
}
