namespace OnlineAuction.Models;

public enum NotificationType
{
    Auction,
    Winning,
    Payment,
    Refund,
    System
}

public class NotificationItemViewModel
{
    public int Id { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FullMessage { get; set; } = string.Empty;

    public string TimeAgo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public string RelatedObjectLabel { get; set; } = string.Empty;

    public string RelatedUrl { get; set; } = string.Empty;

    public string ActionLabel { get; set; } = "Open";

    public string Icon => Type switch
    {
        NotificationType.Auction => "🔨",
        NotificationType.Winning => "🏆",
        NotificationType.Payment => "💳",
        NotificationType.Refund => "↩",
        NotificationType.System => "⚙",
        _ => "🔔"
    };

    public string TypeLabel => Type switch
    {
        NotificationType.Auction => "Auction Update",
        NotificationType.Winning => "Congratulations!",
        NotificationType.Payment => "Payment",
        NotificationType.Refund => "Refund",
        NotificationType.System => "System",
        _ => "Notification"
    };
}

public class NotificationPageViewModel
{
    public List<NotificationItemViewModel> Notifications { get; set; } = [];

    public int UnreadCount => Notifications.Count(notification => !notification.IsRead);
}
