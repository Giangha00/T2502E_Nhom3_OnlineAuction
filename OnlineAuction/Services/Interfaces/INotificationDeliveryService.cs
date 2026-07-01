using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface INotificationDeliveryService
{
    Task DeliverAsync(
        int notificationId,
        CancellationToken cancellationToken = default);

    Task DeliverOutbidAsync(
        int userId,
        string productName,
        int auctionId,
        CancellationToken cancellationToken = default);
}
