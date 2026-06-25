using OnlineAuction.Models;

namespace OnlineAuction.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationItemViewModel?> CreateAndPushAsync(
        int userId,
        string title,
        string message,
        NotificationType type,
        string? relatedUrl,
        string? referenceType = null,
        int? referenceId = null,
        TimeSpan? debounceWindow = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
        int userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

    Task RegisterDeviceTokenAsync(
        int userId,
        string fcmToken,
        string? deviceInfo,
        CancellationToken cancellationToken = default);

    Task UnregisterDeviceTokenAsync(
        int userId,
        string fcmToken,
        CancellationToken cancellationToken = default);

    Task ProcessAuctionEndingSoonNotificationsAsync(CancellationToken cancellationToken = default);
}
