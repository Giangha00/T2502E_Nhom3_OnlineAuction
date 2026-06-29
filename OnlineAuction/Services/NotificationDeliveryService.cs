using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public sealed class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IFcmService _fcmService;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        AuctionHouseDbContext dbContext,
        IFcmService fcmService,
        IRealtimePublisher realtimePublisher,
        ILogger<NotificationDeliveryService> logger)
    {
        _dbContext = dbContext;
        _fcmService = fcmService;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task DeliverAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.DeletedAt == null, cancellationToken);

        if (notification is null)
        {
            return;
        }

        var relatedUrl = InternalUrlValidator.NormalizeOrNull(notification.RelatedUrl) ?? "/";
        var dataPayload = new Dictionary<string, string>
        {
            ["notificationId"] = notification.Id.ToString(),
            ["type"] = notification.Type,
            ["relatedUrl"] = relatedUrl
        };

        try
        {
            await _fcmService.SendToUserAsync(
                notification.UserId,
                notification.Title,
                notification.Message,
                dataPayload,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "FCM push failed for user {UserId}, notification {NotificationId}.",
                notification.UserId,
                notification.Id);
        }

        var viewModel = MapToViewModel(notification);
        var unreadCount = await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                n => n.UserId == notification.UserId && !n.IsRead && n.DeletedAt == null,
                cancellationToken);

        await _realtimePublisher.SendNotificationToUserAsync(
            notification.UserId,
            viewModel,
            unreadCount,
            cancellationToken);
    }

    public async Task DeliverOutbidAsync(
        int userId,
        string productName,
        int auctionId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var recentExists = await _dbContext.Notifications
            .AnyAsync(
                n => n.UserId == userId
                     && n.ReferenceType == NotificationReferenceTypes.AuctionOutbid
                     && n.ReferenceId == auctionId
                     && n.CreatedAt >= cutoff,
                cancellationToken);

        if (recentExists)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var relatedUrl = $"/Auction/Detail/{auctionId}";
        var notification = new Notification
        {
            UserId = userId,
            Title = "You've been outbid",
            Message = $"Someone placed a higher bid on {productName}.",
            Type = NotificationType.Auction.ToString().ToLowerInvariant(),
            RelatedUrl = relatedUrl,
            IsRead = false,
            ReferenceType = NotificationReferenceTypes.AuctionOutbid,
            ReferenceId = auctionId,
            CreatedAt = now
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await DeliverAsync(notification.Id, cancellationToken);
    }

    private static NotificationItemViewModel MapToViewModel(Notification notification) =>
        new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            TimeAgo = RelativeTimeHelper.Format(notification.CreatedAt),
            Type = Enum.TryParse<NotificationType>(notification.Type, true, out var parsedType)
                ? parsedType
                : NotificationType.System,
            RelatedUrl = notification.RelatedUrl,
            IsRead = notification.IsRead
        };
}
