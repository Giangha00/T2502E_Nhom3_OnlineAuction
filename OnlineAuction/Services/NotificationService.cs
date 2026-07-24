using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Messaging;
using OnlineAuction.Messaging.Messages;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class NotificationService : INotificationService
{
    private const int DefaultRecentLimit = 20;
    private const int EndingSoonThresholdMinutes = 60;

    private readonly AuctionHouseDbContext _dbContext;
    private readonly IRabbitMqPublisher _publisher;
    private readonly INotificationDeliveryService _deliveryService;
    private readonly INotificationLocalizer _notifyLocalizer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AuctionHouseDbContext dbContext,
        IRabbitMqPublisher publisher,
        INotificationDeliveryService deliveryService,
        INotificationLocalizer notifyLocalizer,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _deliveryService = deliveryService;
        _notifyLocalizer = notifyLocalizer;
        _logger = logger;
    }

    public async Task<NotificationItemViewModel?> CreateAndPushAsync(
        int userId,
        string title,
        string message,
        NotificationType type,
        string? relatedUrl,
        string? referenceType = null,
        int? referenceId = null,
        TimeSpan? debounceWindow = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        relatedUrl = InternalUrlValidator.NormalizeOrNull(relatedUrl);

        if (debounceWindow.HasValue
            && !string.IsNullOrWhiteSpace(referenceType)
            && referenceId.HasValue)
        {
            var cutoff = DateTime.UtcNow - debounceWindow.Value;
            var recentExists = await _dbContext.Notifications
                .AnyAsync(
                    n => n.UserId == userId
                         && n.ReferenceType == referenceType
                         && n.ReferenceId == referenceId
                         && n.CreatedAt >= cutoff,
                    cancellationToken);

            if (recentExists)
            {
                return null;
            }
        }
        else if (!string.IsNullOrWhiteSpace(referenceType) && referenceId.HasValue)
        {
            var duplicateExists = await _dbContext.Notifications
                .AnyAsync(
                    n => n.UserId == userId
                         && n.ReferenceType == referenceType
                         && n.ReferenceId == referenceId,
                    cancellationToken);

            if (duplicateExists)
            {
                return null;
            }
        }

        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type.ToString().ToLowerInvariant(),
            RelatedUrl = relatedUrl,
            IsRead = false,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = now
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var viewModel = MapToViewModel(notification);
        var deliverMessage = new NotificationDeliverMessage
        {
            NotificationId = notification.Id,
            UserId = userId
        };

        if (!_publisher.TryPublish(RabbitMqQueueNames.NotificationsDeliver, deliverMessage))
        {
            try
            {
                await _deliveryService.DeliverAsync(notification.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Inline notification delivery failed for user {UserId}, notification {NotificationId}.",
                    userId,
                    notification.Id);
            }
        }

        return viewModel;
    }

    public async Task<IReadOnlyList<NotificationItemViewModel>> GetRecentForUserAsync(
        int userId,
        int limit = DefaultRecentLimit,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        limit = Math.Clamp(limit, 1, 50);

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.DeletedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return notifications.Select(MapToViewModel).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        return await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.UserId == userId && n.DeletedAt == null,
                cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (notification.IsRead)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        notification.IsRead = true;
        notification.ReadAt = now;
        notification.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RegisterDeviceTokenAsync(
        int userId,
        string fcmToken,
        string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(fcmToken))
        {
            return;
        }

        fcmToken = fcmToken.Trim();
        var now = DateTime.UtcNow;

        var existing = await _dbContext.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.FcmToken == fcmToken, cancellationToken);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.DeviceInfo = deviceInfo;
            existing.LastUsedAt = now;
        }
        else
        {
            _dbContext.UserDeviceTokens.Add(new UserDeviceToken
            {
                UserId = userId,
                FcmToken = fcmToken,
                DeviceInfo = deviceInfo,
                CreatedAt = now,
                LastUsedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterDeviceTokenAsync(
        int userId,
        string fcmToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            return;
        }

        var token = await _dbContext.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.FcmToken == fcmToken && t.UserId == userId, cancellationToken);

        if (token is null)
        {
            return;
        }

        _dbContext.UserDeviceTokens.Remove(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessAuctionEndingSoonNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(EndingSoonThresholdMinutes);

        var endingSoonAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .Where(a =>
                a.ListingType == ListingTypes.Auction
                && (a.Status == AuctionStatuses.Live || a.Status == AuctionStatuses.EndingSoon)
                && a.DeletedAt == null
                && a.EndDate > now
                && a.EndDate <= threshold)
            .ToListAsync(cancellationToken);

        foreach (var auction in endingSoonAuctions)
        {
            var bidderIds = await _dbContext.Bids
                .AsNoTracking()
                .Where(b => b.AuctionId == auction.Id)
                .Select(b => b.BidderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var registrantIds = await _dbContext.AuctionRegistrations
                .AsNoTracking()
                .Where(r => r.AuctionId == auction.Id && r.Status == AuctionRegistrationStatuses.Approved)
                .Select(r => r.UserId)
                .ToListAsync(cancellationToken);

            var watchlistIds = await _dbContext.WatchlistItems
                .AsNoTracking()
                .Where(w => w.AuctionId == auction.Id)
                .Select(w => w.UserId)
                .ToListAsync(cancellationToken);

            var recipientIds = bidderIds
                .Concat(registrantIds)
                .Concat(watchlistIds)
                .Distinct()
                .ToList();

            var productName = auction.Product?.Name ?? "an auction";
            var relatedUrl = $"/Auction/Detail/{auction.Id}";

            foreach (var userId in recipientIds)
            {
                await CreateAndPushAsync(
                    userId,
                    _notifyLocalizer[NotificationKeys.AuctionEndingSoonTitle],
                    _notifyLocalizer.Format(NotificationKeys.AuctionEndingSoonMessage, productName),
                    NotificationType.Auction,
                    relatedUrl,
                    NotificationReferenceTypes.AuctionEndingSoon,
                    auction.Id,
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task ProcessAuctionStartingSoonNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(EndingSoonThresholdMinutes);

        var startingSoonAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .Include(a => a.Product)
            .Where(a =>
                a.ListingType == ListingTypes.Auction
                && a.RequiresRegistration
                && a.DeletedAt == null
                && a.Status == AuctionStatuses.Scheduled
                && a.StartDate > now
                && a.StartDate <= threshold)
            .ToListAsync(cancellationToken);

        foreach (var auction in startingSoonAuctions)
        {
            var registrantIds = await _dbContext.AuctionRegistrations
                .AsNoTracking()
                .Where(r => r.AuctionId == auction.Id && r.Status == AuctionRegistrationStatuses.Approved)
                .Select(r => r.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var watchlistIds = await _dbContext.WatchlistItems
                .AsNoTracking()
                .Where(w => w.AuctionId == auction.Id)
                .Select(w => w.UserId)
                .ToListAsync(cancellationToken);

            var recipientIds = registrantIds
                .Concat(watchlistIds)
                .Distinct()
                .ToList();

            if (recipientIds.Count == 0)
            {
                continue;
            }

            var productName = auction.Product?.Name ?? "an auction";
            var relatedUrl = $"/Auction/Detail/{auction.Id}";
            var startLocal = DateTimeUtilities.AsUtc(auction.StartDate).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            foreach (var userId in recipientIds)
            {
                await CreateAndPushAsync(
                    userId,
                    _notifyLocalizer[NotificationKeys.AuctionStartingSoonTitle],
                    _notifyLocalizer.Format(NotificationKeys.AuctionStartingSoonMessage, productName, startLocal),
                    NotificationType.Auction,
                    relatedUrl,
                    NotificationReferenceTypes.AuctionStartingSoon,
                    auction.Id,
                    cancellationToken: cancellationToken);
            }
        }
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
