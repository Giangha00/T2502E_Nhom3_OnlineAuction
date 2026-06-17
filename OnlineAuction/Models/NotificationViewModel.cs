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
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedUrl { get; set; }
    public bool IsRead { get; set; }
}
